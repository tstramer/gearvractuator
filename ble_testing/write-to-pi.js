var noble = require('noble');
var os = require('os');
var readline = require('readline');

var serviceUuid = 'ec00';
var characteristicUuid = 'ec0e';

var rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout,
  terminal: false
});

noble.on('stateChange', function(state) {
  if (state === 'poweredOn') {
    //
    // Once the BLE radio has been powered on, it is possible
    // to begin scanning for services. Pass an empty array to
    // scan for all services (uses more time and power).
    //
    console.log('scanning...');
    noble.startScanning([serviceUuid], false);
  } else {
    noble.stopScanning();
  }
})

noble.on('discover', function(peripheral) {
  // we found a peripheral, stop scanning
  noble.stopScanning();

  //
  // The advertisment data contains a name, power level (if available),
  // certain advertised service uuids, as well as manufacturer data,
  // which could be formatted as an iBeacon.
  //
  console.log('found peripheral:', peripheral.advertisement);
  //
  // Once the peripheral has been discovered, then connect to it.
  // It can also be constructed if the uuid is already known.
  ///
  peripheral.connect(function(err) {
    //
    // Once the peripheral has been connected, then discover the
    // services and characteristics of interest.
    //
    peripheral.discoverServices([serviceUuid], function(err, services) {
      services.forEach(function(service) {
        //
        // This must be the service we were looking for.
        //
        console.log('found service:', service.uuid);

        //
        // So, discover its characteristics.
        //
        service.discoverCharacteristics([], function(err, characteristics) {
	  writeToPi(characteristics[0]);
        })
      })
    })
  })
})

function writeToPi(characteristic) {
  console.log("Enter the position of the motor");
  rl.on('line', function(line){
    characteristic.write(intToBuffer(parseInt(line)), false, function(err) {
      if (!err) {
        console.log('write success');
      } else {
        console.log('characteristic write error');
      }
    })
  });
}

function intToBuffer(val) {
  var value = new Buffer(2);
  value.writeInt16LE(val, 0);
  return value;
}
