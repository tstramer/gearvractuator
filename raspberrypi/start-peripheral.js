var bleno = require('bleno');

var BlenoPrimaryService = bleno.PrimaryService;

var MotorControllerCharacteristic = require('./motor-controller-characteristic');

bleno.on('stateChange', function(state) {
  console.log('on -> stateChange: ' + state);

  if (state === 'poweredOn') {
    bleno.startAdvertising('raspberrypi', ['ec00']);
  } else {
    bleno.stopAdvertising();
  }
});

bleno.on('advertisingStart', function(error) {
  console.log('on -> advertisingStart: ' + (error ? 'error ' + error : 'success'));

  if (!error) {
    bleno.setServices([
      new BlenoPrimaryService({
        uuid: 'ec00',
        characteristics: [
          new MotorControllerCharacteristic()
        ]
      })
    ]);
  }
});
