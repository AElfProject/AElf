// named

var defaultHeaders = {
  "User-Agent": "aelf-XMLHttpRequest",
  "Accept": "*/*",
};

let Request = class Request {
    constructor(method, url, async=false, user=null, password=null) {
        this.method = method
        this.url = url
        this.async = async
        this.headers = {}
        var auth = ""
        if (user != null && password != null) {
            auth = 'Basic ' + new Buffer(user + ':' + password).toString('base64')
        } else if (user != null) {
            auth = 'Basic ' + new Buffer(user + ':').toString('base64')
        }
        if (auth.length != 0) {
            this.setHeader("Authorization", auth)
        }
        
        for (var name in defaultHeaders) {
            this.setHeader(name, defaultHeaders[name])
        }
        this.body = null
    }
    
    setHeader(header, value) {
        this.headers[header] = value
    }
    
}
var kUNSENT = 0
var kOPENED = 1
var kHEADERS_RECEIVED = 2
var kLOADING = 3
var kDONE = 4

XMLHttpRequest = class XMLHttpRequest {
  constructor() {
    this.readyState = kUNSENT
    this.onreadystatechange = null
    this.response = null
    this._request = null 
  }
  
  open(method, url, async=false, user=null, password=null) {
    this.abort()
    this._request = new Request(method, url, async, user, password)
    this._setState(kOPENED)
  }
  
  abort() {
    this._request = null
  }
  
  _setState(state) {
    this.readyState = kOPENED
    if (this.onreadystatechange != null) {
      this.onreadystatechange()
    }
  }
  
  setRequestHeader(name, value) {
    this._request.setHeader(name, value)
  }
  
  send(body=null) {
    if (this.readyState !== this.OPENED) {
      throw new Error("INVALID_STATE_ERR: connection must be opened before send() is called");
    }
  }
};