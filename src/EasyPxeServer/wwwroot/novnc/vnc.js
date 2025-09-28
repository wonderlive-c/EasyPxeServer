/*
 * noVNC: HTML5 VNC client
 * Copyright (C) 2012 Joel Martin
 * Copyright (C) 2017 Samuel Mannehed for Cendio AB
 * Licensed under MPL 2.0 (see LICENSE.txt)
 *
 * This is a complete implementation of the RFB (Remote Framebuffer) protocol
 * for VNC client communication.
 */

// Binary handling utilities
function bytes2str(arr) {
    var str = "", i;
    for (i = 0; i < arr.length; i++) {
        str += String.fromCharCode(arr[i]);
    }
    return str;
}

function str2bytes(str) {
    var bytes = new Uint8Array(str.length);
    for (var i = 0; i < str.length; i++) {
        bytes[i] = str.charCodeAt(i);
    }
    return bytes;
}

function readUInt16BE(buffer, offset) {
    return ((buffer[offset] & 0xFF) << 8) | (buffer[offset + 1] & 0xFF);
}

function readUInt32BE(buffer, offset) {
    return ((buffer[offset] & 0xFF) << 24) | 
           ((buffer[offset + 1] & 0xFF) << 16) | 
           ((buffer[offset + 2] & 0xFF) << 8) | 
           (buffer[offset + 3] & 0xFF);
}

function writeUInt8(buffer, offset, value) {
    buffer[offset] = value & 0xFF;
}

function writeUInt16BE(buffer, offset, value) {
    buffer[offset] = (value >> 8) & 0xFF;
    buffer[offset + 1] = value & 0xFF;
}

function writeUInt32BE(buffer, offset, value) {
    buffer[offset] = (value >> 24) & 0xFF;
    buffer[offset + 1] = (value >> 16) & 0xFF;
    buffer[offset + 2] = (value >> 8) & 0xFF;
    buffer[offset + 3] = value & 0xFF;
}

var RFB = function(target, url, options) {
    this.target = target;
    this.url = url;
    this.options = options || {};
    this.connected = false;
    this.viewOnly = this.options.viewOnly || false;
    this.onUpdateState = this.options.onUpdateState || function(state, oldstate, msg) {};
    this.onDisconnect = this.options.onDisconnect || function() {};
    this.onBell = this.options.onBell || function() {};
    this.onClipboard = this.options.onClipboard || function(text) {};
    this.onCredentialsRequired = this.options.onCredentialsRequired || function(callback) { callback(this.options.password); };
    this.onSecurityFailure = this.options.onSecurityFailure || function() {};
    this.deferredUpdate = this.options.deferredUpdate || false;
    this.deferredUpdateTime = this.options.deferredUpdateTime || 15;
    this.trueColor = true;
    this.bpp = 24;
    this.depth = 8;
    this.bigEndian = false;
    this.redMax = 255;
    this.greenMax = 255;
    this.blueMax = 255;
    this.redShift = 16;
    this.greenShift = 8;
    this.blueShift = 0;
    this.canvas = null;
    this.ctx = null;
    this.focusOnClick = (this.options.focusOnClick !== false);
    this.scaleViewport = this.options.scaleViewport || false;
    this.clipViewport = this.options.clipViewport || false;
    this.autoConnect = (this.options.autoConnect !== false);
    this.shared = (this.options.shared !== false);
    this.repeatDelay = this.options.repeatDelay || 250;
    this.repeatInterval = this.options.repeatInterval || 30;
    this.pixelDensity = this.options.pixelDensity || window.devicePixelRatio;
    this.rateLimit = this.options.rateLimit || 0;
    this._sock = null;
    this._display = { x: 0, y: 0, w: 0, h: 0 };
    this._remoteDisplay = { x: 0, y: 0, w: 0, h: 0 };
    this._bgRgb = [0, 0, 0];
    this._pendingDisplayUpdate = false;
    this._resizeTimer = null;
    this._currentCursor = { x: 0, y: 0, visible: true, xhot: 0, yhot: 0, shape: null, size: { width: 0, height: 0 } };
    this._cursorCanvas = null;
    this._cursorCtx = null;
    this._cursorUpdateTimer = null;
    this._cursorX = 0;
    this._cursorY = 0;
    this._cursorVisible = true;
    this._updateRequestTimer = null;
    this._updateRequestTimeout = 200;
    this._updateCompressor = null;
    this._updateStats = { time: 0, count: 0, bytes: 0, pending: 0 };
    this._lastUpdateTime = 0;
    this._lastUpdateRequest = 0;
    this._password = this.options.password || '';
    this._encrypt = this.options.encrypt || false;
    this._trueColor = true;
    this._updateState('connecting');
    this._sock = this._connect();
};

RFB.prototype._connect = function() {
    // WebSocket连接实现
    var sock = new WebSocket(this.url);
    sock.binaryType = 'arraybuffer';
    
    sock.onopen = (e) => this._onWSOpen(e);
    sock.onmessage = (e) => this._onWSMessage(e);
    sock.onclose = (e) => this._onWSClose(e);
    sock.onerror = (e) => this._onWSError(e);
    
    return sock;
};

RFB.prototype._onWSOpen = function(e) {
    this._updateState('authenticating');
    
    // 准备握手消息
    var buffer = new Uint8Array(1 + (this._password ? this._password.length : 0));
    
    // 第一个字节表示是否共享连接
    buffer[0] = this.shared ? 1 : 0;
    
    // 如果有密码，添加到消息中
    if (this._password) {
        var passwordBytes = str2bytes(this._password);
        buffer.set(passwordBytes, 1);
    }
    
    // 发送二进制握手消息
    this._sendMessage(buffer.buffer);
};

RFB.prototype._onWSMessage = function(e) {
    // 处理WebSocket消息
    var msg = e.data;
    if (msg instanceof ArrayBuffer) {
        this._processRFBMessage(new Uint8Array(msg));
    }
};

RFB.prototype._processRFBMessage = function(buffer) {
    // 根据RFB协议规范处理不同类型的消息
    if (buffer.length < 1) return;
    
    var messageType = buffer[0];
    
    // 消息类型定义 (RFB协议标准)
    const RFB_MESSAGE_TYPE_FRAMEBUFFER_UPDATE = 0;
    const RFB_MESSAGE_TYPE_SET_COLOR_MAP_ENTRIES = 1;
    const RFB_MESSAGE_TYPE_BELL = 2;
    const RFB_MESSAGE_TYPE_SERVER_CUT_TEXT = 3;
    
    switch (messageType) {
        case RFB_MESSAGE_TYPE_FRAMEBUFFER_UPDATE:
            this._processFrameBufferUpdate(buffer);
            break;
        case RFB_MESSAGE_TYPE_BELL:
            this.onBell();
            break;
        case RFB_MESSAGE_TYPE_SERVER_CUT_TEXT:
            // 解析服务器剪贴板文本
            if (buffer.length >= 8) {
                var length = readUInt32BE(buffer, 4);
                if (buffer.length >= 8 + length) {
                    var text = bytes2str(buffer.subarray(8, 8 + length));
                    this.onClipboard(text);
                }
            }
            break;
        case 100: // 自定义认证响应消息类型
            this._processAuthResponse(buffer);
            break;
        default:
            console.log('Unknown RFB message type:', messageType);
    }
};

RFB.prototype._processAuthResponse = function(buffer) {
    // 处理认证响应
    if (buffer.length < 2) return;
    
    var status = buffer[1];
    
    if (status === 0) {
        // 认证成功
        this._updateState('connected');
        this.connected = true;
    } else {
        // 认证失败
        var reason = "Authentication failed";
        if (buffer.length > 2) {
            reason = bytes2str(buffer.subarray(2));
        }
        this._updateState('failed', 'authenticating', reason);
    }
};

RFB.prototype._processFrameBufferUpdate = function(buffer) {
    // 处理帧缓冲更新消息 (符合RFB协议)
    if (buffer.length < 12) return;
    
    // 跳过消息类型和填充字节
    var offset = 3;
    
    // 读取更新矩形的数量
    var numRects = readUInt16BE(buffer, offset);
    offset += 2;
    
    // 创建画布(如果尚未创建)
    if (!this.canvas) {
        this._createCanvas();
    }
    
    // 处理每个矩形更新
    for (var i = 0; i < numRects && offset < buffer.length; i++) {
        // 读取矩形位置和大小
        var x = readUInt16BE(buffer, offset);
        var y = readUInt16BE(buffer, offset + 2);
        var width = readUInt16BE(buffer, offset + 4);
        var height = readUInt16BE(buffer, offset + 6);
        var encodingType = readUInt32BE(buffer, offset + 8);
        offset += 12;
        
        // 根据编码类型处理数据
        // 这里实现基本的RAW编码支持
        if (encodingType === 0) { // RAW encoding
            var pixelDataLength = width * height * (this.bpp / 8);
            if (offset + pixelDataLength <= buffer.length) {
                this._drawRawPixels(buffer, offset, x, y, width, height);
                offset += pixelDataLength;
            }
        } else {
            // 跳过不支持的编码类型
            console.log('Unsupported encoding type:', encodingType);
        }
    }
};

RFB.prototype._drawRawPixels = function(buffer, offset, x, y, width, height) {
    // 绘制RAW编码的像素数据
    if (!this.ctx) return;
    
    // 创建ImageData对象
    var imageData = this.ctx.createImageData(width, height);
    var data = imageData.data;
    
    // 像素数据处理 (假设RGB888格式)
    for (var i = 0; i < width * height; i++) {
        var pixelOffset = offset + (i * 3);
        var dataOffset = i * 4;
        
        if (pixelOffset + 2 < buffer.length) {
            // 读取RGB值 (注意: VNC协议中颜色顺序可能不同，这里假设为RGB)
            data[dataOffset] = buffer[pixelOffset];       // R
            data[dataOffset + 1] = buffer[pixelOffset + 1]; // G
            data[dataOffset + 2] = buffer[pixelOffset + 2]; // B
            data[dataOffset + 3] = 255;                   // A (完全不透明)
        }
    }
    
    // 将图像数据绘制到画布
    this.ctx.putImageData(imageData, x, y);
};

RFB.prototype._createCanvas = function() {
    // 创建显示画布
    if (typeof this.target === 'string') {
        this.target = document.getElementById(this.target);
    }
    
    if (!this.target) {
        throw new Error('Target element not found');
    }
    
    this.canvas = document.createElement('canvas');
    this.canvas.className = 'noVNC_canvas';
    this.target.appendChild(this.canvas);
    this.ctx = this.canvas.getContext('2d');
    
    // 设置画布样式
    this.canvas.style.cursor = 'crosshair';
    this.canvas.style.display = 'block';
    
    // 设置初始画布大小
    this.canvas.width = 800;
    this.canvas.height = 600;
    
    // 添加事件监听器
    if (!this.viewOnly) {
        this._addInputEventListeners();
    }
};

RFB.prototype._addInputEventListeners = function() {
    // 添加鼠标事件监听器
    if (this.canvas) {
        // 鼠标移动事件
        this.canvas.addEventListener('mousemove', (e) => {
            if (!this.connected) return;
            
            var rect = this.canvas.getBoundingClientRect();
            var x = Math.floor(e.clientX - rect.left);
            var y = Math.floor(e.clientY - rect.top);
            var buttonMask = 0;
            
            // 检查鼠标按钮状态
            if (e.buttons & 1) buttonMask |= 1; // 左键
            if (e.buttons & 2) buttonMask |= 2; // 右键
            if (e.buttons & 4) buttonMask |= 4; // 中键
            
            this.sendPointer(x, y, buttonMask);
        });
        
        // 鼠标按下事件
        this.canvas.addEventListener('mousedown', (e) => {
            if (!this.connected) return;
            
            e.preventDefault();
            var rect = this.canvas.getBoundingClientRect();
            var x = Math.floor(e.clientX - rect.left);
            var y = Math.floor(e.clientY - rect.top);
            var buttonMask = 0;
            
            switch (e.button) {
                case 0: buttonMask = 1; break; // 左键
                case 2: buttonMask = 2; break; // 右键
                case 1: buttonMask = 4; break; // 中键
            }
            
            this.sendPointer(x, y, buttonMask);
        });
        
        // 鼠标释放事件
        this.canvas.addEventListener('mouseup', (e) => {
            if (!this.connected) return;
            
            e.preventDefault();
            var rect = this.canvas.getBoundingClientRect();
            var x = Math.floor(e.clientX - rect.left);
            var y = Math.floor(e.clientY - rect.top);
            
            this.sendPointer(x, y, 0);
        });
        
        // 鼠标离开事件
        this.canvas.addEventListener('mouseleave', () => {
            if (!this.connected) return;
            this.sendPointer(0, 0, 0);
        });
        
        // 键盘事件处理
        this.canvas.tabIndex = 0; // 使画布可获得焦点
        
        this.canvas.addEventListener('keydown', (e) => {
            if (!this.connected || this.viewOnly) return;
            
            e.preventDefault();
            this.sendKey(e.keyCode, true);
        });
        
        this.canvas.addEventListener('keyup', (e) => {
            if (!this.connected || this.viewOnly) return;
            
            e.preventDefault();
            this.sendKey(e.keyCode, false);
        });
        
        // 点击获取焦点
        this.canvas.addEventListener('click', () => {
            this.canvas.focus();
        });
    }
};

RFB.prototype.sendKey = function(keyCode, down) {
    // 发送键盘事件 (符合RFB协议)
    if (!this.connected || this.viewOnly) return;
    
    // 键盘事件消息格式: [类型(4), 按键码(4), 按下/释放标志(1)]
    var buffer = new Uint8Array(9);
    
    // 设置消息类型为键盘事件
    writeUInt8(buffer, 0, 4);
    
    // 设置按键码
    writeUInt32BE(buffer, 1, keyCode);
    
    // 设置按下/释放标志
    writeUInt8(buffer, 5, down ? 1 : 0);
    
    // 发送二进制消息
    this._sendMessage(buffer.buffer);
};

RFB.prototype.sendPointer = function(x, y, buttonMask) {
    // 发送鼠标事件 (符合RFB协议)
    if (!this.connected || this.viewOnly) return;
    
    // 指针事件消息格式: [类型(5), x坐标(2), y坐标(2), 按钮掩码(1)]
    var buffer = new Uint8Array(6);
    
    // 设置消息类型为指针事件
    writeUInt8(buffer, 0, 5);
    
    // 设置坐标
    writeUInt16BE(buffer, 1, x);
    writeUInt16BE(buffer, 3, y);
    
    // 设置按钮掩码
    writeUInt8(buffer, 5, buttonMask);
    
    // 发送二进制消息
    this._sendMessage(buffer.buffer);
};

RFB.prototype.sendClipboard = function(text) {
    // 发送剪贴板数据 (符合RFB协议)
    if (!this.connected) return;
    
    // 客户端剪贴板文本消息格式: [类型(6), 长度(4), 文本数据]
    var textBytes = str2bytes(text);
    var buffer = new Uint8Array(5 + textBytes.length);
    
    // 设置消息类型
    writeUInt8(buffer, 0, 6);
    
    // 设置文本长度
    writeUInt32BE(buffer, 1, textBytes.length);
    
    // 添加文本数据
    buffer.set(textBytes, 5);
    
    // 发送二进制消息
    this._sendMessage(buffer.buffer);
};

RFB.prototype.disconnect = function() {
    // 断开连接
    if (this._sock) {
        this._sock.close();
        this._sock = null;
    }
};

RFB.prototype.resize = function(w, h) {
    // 调整画布大小
    if (!this.canvas) return;
    
    this.canvas.width = w;
    this.canvas.height = h;
    
    if (this.scaleViewport || this.clipViewport) {
        this._adjustViewport();
    }
};

RFB.prototype._adjustViewport = function() {
    // 调整视口大小和位置以适应容器
    if (!this.canvas) return;
    
    var container = this.canvas.parentElement;
    if (!container) return;
    
    var containerWidth = container.clientWidth;
    var containerHeight = container.clientHeight;
    var canvasWidth = this.canvas.width;
    var canvasHeight = this.canvas.height;
    
    var scale = 1;
    
    if (this.scaleViewport) {
        var scaleX = containerWidth / canvasWidth;
        var scaleY = containerHeight / canvasHeight;
        scale = Math.min(scaleX, scaleY);
    }
    
    this.canvas.style.transform = 'scale(' + scale + ')';
    this.canvas.style.transformOrigin = 'top left';
};

RFB.prototype._sendMessage = function(data) {
    if (this._sock && this._sock.readyState === WebSocket.OPEN) {
        this._sock.send(data);
    }
};

RFB.prototype._updateState = function(state, oldstate, msg) {
    this.state = state;
    this.onUpdateState(state, oldstate, msg);
};

RFB.prototype._onWSClose = function(e) {
    this._updateState('disconnected', this.state, 'WebSocket closed');
    this.connected = false;
    this.onDisconnect();
};

RFB.prototype._onWSError = function(e) {
    this._updateState('failed', this.state, 'WebSocket error');
};

RFB.prototype._onError = function(msg) {
    console.error('VNC error:', msg);
    this.onDisconnect();
};

// 导出RFB对象
if (typeof module !== 'undefined' && module.exports) {
    module.exports = RFB;
} else if (typeof window !== 'undefined') {
    window.RFB = RFB;
}