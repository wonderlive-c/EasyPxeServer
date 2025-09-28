// noVNC JavaScript辅助函数

// 存储活动的VNC连接
const activeConnections = {};

// 检查元素是否存在
window.checkElementExists = function(elementId) {
    try {
        const element = document.getElementById(elementId);
        return element !== null;
    } catch (error) {
        console.error('Error checking element existence:', error);
        return false;
    }
};

// noVNC JavaScript辅助函数
// 注：本应用使用独立的noVNC文件（位于wwwroot/novnc目录），而非直接使用npm包中的模块
// 原因是：ASP.NET Core默认只从wwwroot目录提供静态文件，无法直接访问node_modules目录
// 如需使用npm包版本，需要配置静态文件中间件或使用构建工具将文件复制到wwwroot目录

// 加载noVNC的CSS和JS文件
window.loadNoVNC = async function() {
    // 检查是否已经加载了noVNC
    if (window.RFB) {
        console.log('noVNC already loaded');
        return;
    }

    try {
        // 首先尝试从npm包加载noVNC文件
        // 加载CSS文件
        const cssLink = document.createElement('link');
        cssLink.rel = 'stylesheet';
        cssLink.href = '/novnc/vnc.css'; // 仍然使用项目中的CSS文件
        document.head.appendChild(cssLink);

        // 加载JS文件 - 先尝试从npm包加载
        await loadScript('/node_modules/novnc/lib/rfb.js');
        console.log('noVNC loaded successfully from npm package');
    } catch (npmError) {
        console.warn('Failed to load noVNC from npm package, falling back to local files:', npmError);
        try {
            // 加载noVNC的CSS文件 - 使用本地文件作为回退
            const cssLink = document.createElement('link');
            cssLink.rel = 'stylesheet';
            cssLink.href = '/novnc/vnc.css';
            document.head.appendChild(cssLink);

            // 加载noVNC的JS文件 - 使用本地文件作为回退
            await loadScript('/novnc/vnc.js');
            console.log('noVNC loaded successfully from local files');
        } catch (localError) {
            console.error('Failed to load noVNC from local files:', localError);
            throw localError;
        }
    }
};

// 辅助函数：加载单个脚本文件
function loadScript(src) {
    return new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = src;
        script.onload = resolve;
        script.onerror = reject;
        document.head.appendChild(script);
    });
};

// 初始化VNC连接
window.initNoVNC = function(targetId, url, connectionId, dotNetRef, password) {
    try {
        // 获取目标元素
        const targetElement = document.getElementById(targetId);
        if (!targetElement) {
            console.error('Target element not found:', targetId);
            // 创建一个临时消息元素显示错误信息
            const errorElement = document.createElement('div');
            errorElement.className = 'noVNC_error';
            errorElement.innerHTML = `<div style="padding: 20px; background-color: #f8d7da; color: #721c24; border: 1px solid #f5c6cb; border-radius: 5px;">
                <h4>显示错误</h4>
                <p>无法找到显示元素: ${targetId}</p>
                <p>请确保您的VNC连接配置正确，并尝试刷新页面。</p>
            </div>`;
            
            // 找到父容器并添加错误消息
            const parentContainer = document.getElementsByClassName('novnc-display-container')[0];
            if (parentContainer) {
                parentContainer.appendChild(errorElement);
            }
            
            dotNetRef.invokeMethodAsync('HandleError', '无法找到显示元素: ' + targetId);
            return;
        }

        // 创建VNC连接选项
        const options = {
            password: password,
            viewOnly: false,
            shared: true,
            binary: true,  // 明确启用二进制协议，这对于VNC RFB协议至关重要
            onUpdateState: function(state, oldstate, msg) {
                console.log('VNC state changed:', oldstate, '->', state, msg);
                
                switch (state) {
                    case 'connected':
                        dotNetRef.invokeMethodAsync('HandleConnect');
                        break;
                    case 'disconnected':
                        dotNetRef.invokeMethodAsync('HandleDisconnect');
                        break;
                    case 'failed':
                        dotNetRef.invokeMethodAsync('HandleError', msg || 'Connection failed');
                        break;
                }
            },
            onDisconnect: function() {
                console.log('VNC disconnected');
                dotNetRef.invokeMethodAsync('HandleDisconnect');
            },
            onBell: function() {
                console.log('VNC bell');
                dotNetRef.invokeMethodAsync('HandleBell');
            },
            onClipboard: function(text) {
                console.log('Clipboard text:', text);
                dotNetRef.invokeMethodAsync('HandleClipboard', text);
            },
            onCredentialsRequired: function(callback) {
                // 如果需要密码但未提供，请求用户输入
                if (!password) {
                    const inputPassword = prompt('请输入VNC密码:');
                    callback(inputPassword);
                } else {
                    callback(password);
                }
            },
            onSecurityFailure: function() {
                console.error('VNC security failure');
                dotNetRef.invokeMethodAsync('HandleError', 'Security failure');
            }
        };

        // 创建RFB对象并存储连接
        const rfb = new RFB(targetElement, url, options);
        activeConnections[connectionId] = rfb;

        // 设置连接属性
        rfb.viewOnly = options.viewOnly;
        rfb.scaleViewport = true;
        rfb.clipViewport = true;
        rfb.focusOnClick = true;

        console.log('noVNC initialized for connection:', connectionId);
    } catch (error) {
        console.error('Error initializing noVNC:', error);
        dotNetRef.invokeMethodAsync('HandleError', error.message || 'Failed to initialize VNC connection');
    }
};

// 断开VNC连接
window.disconnectNoVNC = function(connectionId) {
    try {
        const rfb = activeConnections[connectionId];
        if (rfb) {
            rfb.disconnect();
            delete activeConnections[connectionId];
            console.log('VNC connection disconnected:', connectionId);
        }
    } catch (error) {
        console.error('Error disconnecting VNC:', error);
    }
};

// 发送键盘事件到VNC服务器
window.sendKeyToVNC = function(connectionId, key, down) {
    try {
        const rfb = activeConnections[connectionId];
        if (rfb && rfb.connected) {
            rfb.sendKey(key, down);
        }
    } catch (error) {
        console.error('Error sending key to VNC:', error);
    }
};

// 发送鼠标事件到VNC服务器
window.sendPointerToVNC = function(connectionId, x, y, buttonMask) {
    try {
        const rfb = activeConnections[connectionId];
        if (rfb && rfb.connected) {
            rfb.sendPointer(x, y, buttonMask);
        }
    } catch (error) {
        console.error('Error sending pointer to VNC:', error);
    }
};

// 发送剪贴板数据到VNC服务器
window.sendClipboardToVNC = function(connectionId, text) {
    try {
        const rfb = activeConnections[connectionId];
        if (rfb && rfb.connected) {
            rfb.sendClipboard(text);
        }
    } catch (error) {
        console.error('Error sending clipboard to VNC:', error);
    }
};

// 调整VNC视口大小
window.resizeVNC = function(connectionId, width, height) {
    try {
        const rfb = activeConnections[connectionId];
        if (rfb) {
            rfb.resize(width, height);
        }
    } catch (error) {
        console.error('Error resizing VNC viewport:', error);
    }
};

// 获取所有活动的VNC连接
window.getActiveVNCConnections = function() {
    return Object.keys(activeConnections);
};

// 检查连接是否存在
window.hasVNCConnection = function(connectionId) {
    return connectionId in activeConnections;
};

// 清理所有VNC连接（页面卸载时调用）
window.cleanupVNCConnections = function() {
    Object.keys(activeConnections).forEach(connectionId => {
        try {
            activeConnections[connectionId].disconnect();
        } catch (error) {
            console.error('Error during VNC cleanup:', error);
        }
    });
    
    activeConnections = {};
    console.log('All VNC connections cleaned up');
};

// 监听页面卸载事件以清理VNC连接
window.addEventListener('beforeunload', cleanupVNCConnections);

// 导出函数供其他脚本使用
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        loadNoVNC,
        initNoVNC,
        disconnectNoVNC,
        sendKeyToVNC,
        sendPointerToVNC,
        sendClipboardToVNC,
        resizeVNC,
        getActiveVNCConnections,
        hasVNCConnection,
        cleanupVNCConnections
    };
}