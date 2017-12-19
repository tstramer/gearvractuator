var util = require('util');
var bleno = require('bleno');

var StepperControllerBridge = require('./stepper-controller-bridge');

var BlenoCharacteristic = bleno.Characteristic;

var MotorControllerCharacteristic = function() {
  MotorControllerCharacteristic.super_.call(this, {
    uuid: 'ec0e',
    properties: ['read', 'write', 'notify'],
    value: 0
  });

  this._value = new Buffer(0);
  this._updateValueCallback = null;
};

util.inherits(MotorControllerCharacteristic, BlenoCharacteristic);

MotorControllerCharacteristic.prototype.onReadRequest = function(offset, callback) {
  console.log('MotorControllerCharacteristic - onReadRequest: value = ' + this._value.toString('hex'));

  callback(this.RESULT_SUCCESS, this._value);
};

MotorControllerCharacteristic.prototype.onWriteRequest = function(data, offset, withoutResponse, callback) {
  StepperControllerBridge.writeValue(data);
  this._value = data;

  console.log('MotorControllerCharacteristic - onWriteRequest: value = ' + this._value.toString('hex'));

  if (this._updateValueCallback) {
    console.log('MotorControllerCharacteristic - onWriteRequest: notifying');

    this._updateValueCallback(this._value);
  }

  callback(this.RESULT_SUCCESS);
};

MotorControllerCharacteristic.prototype.onSubscribe = function(maxValueSize, updateValueCallback) {
  console.log('MotorControllerCharacteristic - onSubscribe');

  this._updateValueCallback = updateValueCallback;
};

MotorControllerCharacteristic.prototype.onUnsubscribe = function() {
  console.log('MotorControllerCharacteristic - onUnsubscribe');

  this._updateValueCallback = null;
};

module.exports = MotorControllerCharacteristic;
