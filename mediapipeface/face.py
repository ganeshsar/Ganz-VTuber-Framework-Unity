# MediaPipe Face
import mediapipe as mp
from mediapipe.tasks import python
from mediapipe.tasks.python import vision
import numpy as np

BaseOptions = mp.tasks.BaseOptions
FaceLandmarker = mp.tasks.vision.FaceLandmarker
FaceLandmarkerOptions = mp.tasks.vision.FaceLandmarkerOptions
FaceLandmarkerResult = mp.tasks.vision.FaceLandmarkerResult
VisionRunningMode = mp.tasks.vision.RunningMode

import cv2
import threading
import time, struct
import global_vars 
from capturer import CaptureThread
from visualization import draw_landmarks_on_image
from helpers import Point, get_normal,rotation2euler, translation

# the body thread actually does the 
# processing of the captured images, and communication with unity
class FaceThread(threading.Thread):
    data = ""
    dirty = True
    pipe = None
    timeSinceCheckedConnection = 0
    timeSincePostStatistics = 0
    latestResult = None
    resultSubscribers = [] #expects functions with FaceLandmarkerResult input

    def callback_result(self, result: FaceLandmarkerResult, output_image: mp.Image, timestamp_ms: int):
        try:
            self.latestResult = result

            if len(result.face_blendshapes)>0:
                self.data = ""
   
                n=rotation2euler(result.facial_transformation_matrixes[0])
                self.data += "%f|%f|%f\n" % (n[0],n[1],n[2])
                t=translation(result.facial_transformation_matrixes[0])
                self.data += "%f|%f|%f\n" % (t[0],t[1],t[2])

                for v in result.face_blendshapes[0]:
                    i = v.index
                    s = v.score
                    if s<.0001:
                        s = 0
                    self.data += "{}|{}\n".format(i,s)

                if self.pipe != None:
                    try:     
                        s = self.data.encode('utf-8') 
                        self.pipe.write(struct.pack('I', len(s)) + s)   
                        self.pipe.seek(0)    
                    except Exception as ex:  
                        print("Failed to write to pipe. Is the unity project open?")
                        self.pipe= None

                for s in self.resultSubscribers:
                    s(result)
        except Exception as ex:
            print(ex)

    def run(self):
        capture = CaptureThread()
        capture.start()

        base_options = python.BaseOptions(model_asset_path='face_landmarker.task')
        options = vision.FaceLandmarkerOptions(base_options=base_options,
                                       output_face_blendshapes=True,
                                       output_facial_transformation_matrixes=True,
                                       num_faces=1,
                                       running_mode=VisionRunningMode.LIVE_STREAM,
                                       result_callback=self.callback_result)

        with vision.FaceLandmarker.create_from_options(options) as landmarker: 
            while not global_vars.KILL_THREADS and capture.isRunning==False:
                print("Waiting for camera and capture thread.")
                time.sleep(0.5)
            print("Beginning capture")
                
            while not global_vars.KILL_THREADS and capture.cap.isOpened():
                ti = time.time()

                # Fetch stuff from the capture thread
                #NOTE: this approach may not be great anymore, maybe block thread with capture.
                image = capture.frame
                mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=image)
                                
                image.flags.writeable = global_vars.DEBUG
                
                # Detections
                landmarker.detect_async(mp_image, int(time.time()* 1000))
                tf = time.time()

                # STEP 5: Process the detection result. In this case, visualize it.
                annotated_image = image
                if self.latestResult != None:
                    annotated_image = draw_landmarks_on_image(image, self.latestResult)
                rgb_annotated_image = annotated_image
                cv2.imshow('Face Tracking', rgb_annotated_image)
                cv2.waitKey(3)

                if self.pipe==None and time.time()-self.timeSinceCheckedConnection>=1:
                    try:
                        p = open(r'\\.\pipe\UnityMediaPipeFace', 'r+b', 0)
                        self.pipe = p
                    except Exception as ex:
                        print(str(ex))
                        print("Waiting for Unity project to run...")
                        self.pipe = None
                    self.timeSinceCheckedConnection = time.time()

                time.sleep(1/1000)
                        
        self.pipe.close()
        capture.cap.release()
        cv2.destroyAllWindows()