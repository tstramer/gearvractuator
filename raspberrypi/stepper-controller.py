#!/usr/bin/env python

#Basic imports
from ctypes import *
import sys
from time import sleep

#Phidget specific imports
from Phidgets.PhidgetException import PhidgetErrorCodes, PhidgetException
from Phidgets.Events.Events import AttachEventArgs, DetachEventArgs, ErrorEventArgs, InputChangeEventArgs, CurrentChangeEventArgs, StepperPositionChangeEventArgs, VelocityChangeEventArgs
from Phidgets.Devices.Stepper import Stepper
from Phidgets.Phidget import PhidgetLogLevel
import atexit
import time
import threading


STEPPER_ACCELERATION = 350543
STEPPER_VELOCITY = 230200
STEPPER_CURRENT = 2.0

CONTROL_SIGNAL_RESET_STEPPER = -1
CONTROL_SIGNAL_DISENGAGE_STEPPER = -2
CONTROL_SIGNAL_ENGAGE_STEPPER = -3
CONTROL_SIGNAL_CREATE_NOISE = -4

STEPPER_MAX_POSITION = 5500

MAX_IDLE_TIME_SECS = 120

STEPPER_NOISE_STEPS = 50

lastStepTimeSecs = -1


try:
    stepper = Stepper()
except RuntimeError as e:
    print("Runtime Exception: %s" % e.details)
    print("Exiting....")
    exit(1)

#Information Display Function
def DisplayDeviceInfo():
    print("|------------|----------------------------------|--------------|------------|")
    print("|- Attached -|-              Type              -|- Serial No. -|-  Version -|")
    print("|------------|----------------------------------|--------------|------------|")
    print("|- %8s -|- %30s -|- %10d -|- %8d -|" % (stepper.isAttached(), stepper.getDeviceName(), stepper.getSerialNum(), stepper.getDeviceVersion()))
    print("|------------|----------------------------------|--------------|------------|")
    print("Number of Motors: %i" % (stepper.getMotorCount()))

#Event Handler Callback Functions
def StepperAttached(e):
    attached = e.device
    print("Stepper %i Attached!" % (attached.getSerialNum()))

def StepperDetached(e):
    detached = e.device
    print("Stepper %i Detached!" % (detached.getSerialNum()))

def StepperError(e):
    try:
        source = e.device
        print("Stepper %i: Phidget Error %i: %s" % (source.getSerialNum(), e.eCode, e.description))
    except PhidgetException as e:
        print("Phidget Exception %i: %s" % (e.code, e.details))

def StepperCurrentChanged(e):
    source = e.device
    print("Stepper %i: Motor %i -- Current Draw: %6f" % (source.getSerialNum(), e.index, e.current))

def StepperInputChanged(e):
    source = e.device
    print("Stepper %i: Input %i -- State: %s" % (source.getSerialNum(), e.index, e.state))

def StepperPositionChanged(e):
    source = e.device
    print("Stepper %i: Motor %i -- Position: %f" % (source.getSerialNum(), e.index, e.position))

def StepperVelocityChanged(e):
    source = e.device
    print("Stepper %i: Motor %i -- Velocity: %f" % (source.getSerialNum(), e.index, e.velocity))

def ResetStepper():
    print("Set the motor as de-engaged...")
    stepper.setEngaged(0, False)

    print("Set the current position as max position...")
    stepper.setCurrentPosition(0, STEPPER_MAX_POSITION)
    sleep(1)
    
    print("Set the motor as engaged...")
    stepper.setEngaged(0, True)
    sleep(1)

    print("Reset stepper back to initial position...")
    StepTo(0)
    

def InitStepper():
    try:
        ResetStepper();
        stepper.setAcceleration(0, STEPPER_ACCELERATION)
        stepper.setVelocityLimit(0, STEPPER_VELOCITY)
        stepper.setCurrentLimit(0, STEPPER_CURRENT)
    except PhidgetException as e:
        print("Phidget Exception %i: %s" % (e.code, e.details))
        print("Exiting....")
        exit(1)
        
def DeInitStepper():
    try:
        stepper.setEngaged(0, False)
        sleep(1)
        stepper.closePhidget()
    except PhidgetException as e:
        print("Phidget Exception %i: %s" % (e.code, e.details))
        print("Exiting....")
        exit(1)

def EngageStepper():
    stepper.setEngaged(0, True)

def DisengageStepper():
    stepper.setEngaged(0, False)

def StepTo(position):
    try: 
        global lastStepTimeSecs
        lastStepTimeSecs = time.time()
	EngageStepper()
        print("Will now move to position %s..." % position)
        stepper.setTargetPosition(0, position)
        while stepper.getCurrentPosition(0) != position:
            pass
    except PhidgetException as e:
        print("Phidget Exception %i: %s" % (e.code, e.details))
        print("Exiting....")
        exit(1)

def CreateNoise():
    currPos = stepper.getCurrentPosition(0)
    StepTo(currPos + STEPPER_NOISE_STEPS);
    StepTo(currPos);



def OnExit():
    DeInitStepper()

def SetInterval(interval):
    def decorator(function):
        def wrapper(*args, **kwargs):
            stopped = threading.Event()

            def loop(): # executed in another thread
                while not stopped.wait(interval): # until stopped
                    function(*args, **kwargs)

            t = threading.Thread(target=loop)
            t.daemon = True # stop if the program exits
            t.start()
            return stopped
        return wrapper
    return decorator

@SetInterval(60)
def CheckStepperIdle():
    global lastStepTimeSecs
    sys.stdout.flush()
    if time.time() - lastStepTimeSecs > MAX_IDLE_TIME_SECS:
        print("Stepper idle, disengaging...")
        sys.stdout.flush()
        DisengageStepper()


#Main Program Code
try:
    #stepper.enableLogging(PhidgetLogLevel.PHIDGET_LOG_VERBOSE, "phidgetlog.log")
    stepper.setOnAttachHandler(StepperAttached)
    stepper.setOnDetachHandler(StepperDetached)
    stepper.setOnErrorhandler(StepperError)
    stepper.setOnCurrentChangeHandler(StepperCurrentChanged)
    stepper.setOnInputChangeHandler(StepperInputChanged)
    stepper.setOnPositionChangeHandler(StepperPositionChanged)
    stepper.setOnVelocityChangeHandler(StepperVelocityChanged)
    atexit.register(OnExit);
except PhidgetException as e:
    print("Phidget Exception %i: %s" % (e.code, e.details))
    print("Exiting....")
    exit(1)

print("Opening phidget object....")

try:
    stepper.openPhidget()
except PhidgetException as e:
    print("Phidget Exception %i: %s" % (e.code, e.details))
    print("Exiting....")
    exit(1)

print("Waiting for attach....")

try:
    stepper.waitForAttach(10000)
except PhidgetException as e:
    print("Phidget Exception %i: %s" % (e.code, e.details))
    try:
        stepper.closePhidget()
    except PhidgetException as e:
        print("Phidget Exception %i: %s" % (e.code, e.details))
        print("Exiting....")
        exit(1)
    print("Exiting....")
    exit(1)
else:
    DisplayDeviceInfo()

    InitStepper()

    lastStepTimeSecs = time.time()
    CheckStepperIdle()

    sys.stdout.flush()

    while True:
        line = sys.stdin.readline()
 	print("Received on stdin: %s" % line)
        sys.stdout.flush();
        val = int(line)
        if val == CONTROL_SIGNAL_RESET_STEPPER:
	    ResetStepper()
        elif val == CONTROL_SIGNAL_DISENGAGE_STEPPER:
            DisengageStepper()
        elif val == CONTROL_SIGNAL_ENGAGE_STEPPER:
	    EngageStepper()
        elif val == CONTROL_SIGNAL_CREATE_NOISE:
	    CreateNoise()
	else:
            StepTo(val);
