#!/bin/sh

sudo hciconfig hci0 up
sudo hciconfig hci0 sspmode 1
sudo hciconfig hci0 piscan
sudo node ./start-peripheral.js
