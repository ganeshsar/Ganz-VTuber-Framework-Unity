#pipe server
from face import FaceThread
import time
import struct
import global_vars
from sys import exit

thread = FaceThread()
thread.start()

i = input()
print("Exiting…")        
global_vars.KILL_THREADS = True
time.sleep(0.5)
exit()