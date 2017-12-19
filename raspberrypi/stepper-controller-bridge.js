var PythonShell = require('python-shell');

var SCRIPT_NAME = 'stepper-controller.py';

var MotorControllerBridge = function() {
  this._pyshell = new PythonShell(SCRIPT_NAME);
  this._pyshell.on('message', function (message) {
    console.log('stepper-controller.py: ' + message);
  });
}

MotorControllerBridge.prototype.exit = function() {
  // end the input stream and allow the process to exit
  this._pyshell.end(function (err) {
    if (err) throw err;
    console.log('finished');
  });
}

MotorControllerBridge.prototype.writeValue = function(buf) {
  this._pyshell.send(buf.readInt16LE().toString());
}

module.exports = new MotorControllerBridge();
