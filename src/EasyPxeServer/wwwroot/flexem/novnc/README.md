# Welcome to novnc

## This project was forked from novnc ([github](https://github.com/novnc/noVNC))

### npm install
``` bash
npm install @flexem/novnc --save
```

### Usage & Steps
1. 获取VNC接口Token
``` ts
getVncToken(): Observable<VncTokenDto> {
    const self = this;
    const url = (!self.appConfig.vncUrl ? self.appConfig.asApiUrl + '/vnc' : self.appConfig.vncUrl) + '/api/server';
    const ret: VncTokenDto = new VncTokenDto();
    return self.httpClient.get(url).map((response) => {
      ret.id = response.id;
      ret.ip = response.ip;
      ret.port = response.port;
      ret.token = response.token;
      return ret;
    }).catch((error: HttpError) => {
      return Observable.throw(error);
    });
  }
```

2. 调用FBOX接口开启VNC连接
``` ts
openVnc(box: BoxRegistration, param: OpenVncDto): Observable<boolean> {
    const self = this;
    const url_ = box.cs.apiBaseUrl + 'v2/box/' + box.id + '/openvnc';
    return self.httpClient.post(url_, param).map((response) => {
      return true;
    }).catch((error: HttpError) => {
      return Observable.throw(error);
    });
  }
```

3. 接收FBOX Signalr返回的开启VNC状态
``` ts
this.hubProxy.on('openVncStatus', (boxNo, id, status, token) => {
      const self = this;
      self.events.publish('openVncStatus', boxNo, id, status, token);
    });
    
error code
"1": "设备网络不通,请检查设备VNC服务是否开启"
"2": "服务器网络不通"
"3": "校验码验证无效"
"4": "设备连接数过多"
```

4. html body添加div
``` html
<div id="vnccreen"></div>
```

5. js调用RFB
``` ts
connect() {
    const self = this;
    const url = (!self.boxInformationProxy.appConfig.vncUrl ? self.boxInformationProxy.appConfig.asApiUrl
       : self.boxInformationProxy.appConfig.vncUrl).replace('http://', 'ws://').replace('https://', 'wss://')
       + '/websockify?token=' + self.vncParams.token;
    const options = {
      credentials: null
    };
    if (self.vncParams.password) {
        options.credentials = { password: self.vncParams.password };
    }
    self.rfb = new RFB(document.getElementById('vnccreen'), url, options);

    // Add listeners to important events from the RFB module
    self.rfb.addEventListener('connect', (e) => {
      self.connectedToServer(e, self);
    });
    self.rfb.addEventListener('disconnect', (e) => {
      self.disconnectedFromServer(e, self);
    });
    self.rfb.addEventListener('credentialsrequired', (e) => {
      self.credentialsAreRequired(e, self);
    });
    self.rfb.addEventListener('securityfailure', (e) => {
      self.securityFailed(e, self);
    });
    self.rfb.addEventListener('desktopname', (e) => {
      self.updateDesktopName(e, self);
    });

    // Set parameters that can be changed on an active connection
    // self.rfb.viewOnly = self.vncParams.viewonly === true;
    self.rfb.scaleViewport = self.vncParams.scale === true;
    self.rfb.qualityLevel = 1;
    // self.rfb.dragViewport = true;
    self.rfb.dragDelayMs = 300;
    self.rfb._keyboard.onkeyevent = self.onKeyEvent;
  }
  connectedToServer(e, self) {
    self.status(self.l('VNC.VncConnectedTo') + self.desktopName);
  }
  disconnectedFromServer(e, self) {
    if (e.detail.clean) {
        self.status(self.l('VNC.VncConnectionClosed'));
    } else {
        self.showAlert(self.l('VNC.VncConnectionError'), self.l('VNC.VncConnectionErrorAlert'), true);
    }
  }
  credentialsAreRequired(e, self) {
    const prompt = this.alertCtrl.create({
      title: self.l('VNC.VncPasswordPrompt'),
      message: '',
      inputs: [
        {
          name: 'Password',
          placeholder: self.l('VNC.VncPassword')
        }
      ],
      buttons: [
        {
          text: self.l('Cancel'),
          handler: (data) => {
            self.navCtrl.pop();
          }
        },
        {
          text: self.l('OK'),
          handler: (data) => {
            self.rfb.sendCredentials({ password: data.Password });
          }
        }
      ]
    });
    prompt.present();
  }
  securityFailed(e, self) {
    let msg = '';
    if ('reason' in e.detail) {
        msg = 'New connection has been rejected with reason: ' + e.detail.reason;
    } else {
        msg = 'New connection has been rejected';
    }
    self.showAlert(msg, null, true);
  }
  updateDesktopName(e, self) {
    self.desktopName = e.detail.name;
  }
  status(text) {
    super.toastl(text);
  }
  onKeyEvent(keysym: any, code: any, down: any): void {
  }

  showAlert(title: string, subTitle?: string, back = false): void {
    const self = this;
    const alert = self.alertCtrl.create({
      title: title || 'Message',
      subTitle: subTitle,
      buttons: [self.l('OK')]
    });
    alert.present();
    if (back) {
      self.navCtrl.pop();
    }
  }
```

