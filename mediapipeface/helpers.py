import math

class Point:
    def __init__(self,m) -> None:

        pass
    def __init__(self,x,y=0,z=0) -> None:
        if isinstance(x, float):
            self.x = x
            self.y = y
            self.z = z
        else:
            self.x = x.x
            self.y = x.y
            self.z = x.z
        pass
    def __add__(self,other):
        return Point(self.x+other.x,self.y+other.y,self.z+other.z)
    
    def __sub__(self,other):
        return Point(self.x-other.x,self.y-other.y,self.z-other.z)
def get_normal(p1,p2,p3):
    u = p2-p1
    v = p3-p1
    n = ((u.y*v.z-u.z*v.y),(u.z*v.x-u.x*v.z),(u.x*v.y-u.y*v.x))
    nl=math.sqrt(n[0]*n[0]+n[1]*n[1]+n[2]*n[2])
    return (n[0]/nl,n[1]/nl,n[2]/nl)
def get_angle(n,v):
    v = (1,0,0)
    d = n[0]*v[0]+n[1]*v[1]+n[2]*v[2]
    return math.acos(d)*(180.0/math.pi)

import sys
def translation(M):
    return(M[0,3],M[1,3],M[2,3])
def rotation2euler(R):
    #https://www.meccanismocomplesso.org/en/3d-rotations-and-euler-angles-in-python/
    tolerance = sys.float_info.epsilon * 10
    
    if abs(R[0,0])< tolerance and abs(R[1,0]) < tolerance:
        eul1 = 0
        eul2 = math.atan2(-R[2,0], R[0,0])
        eul3 = math.atan2(-R[1,2], R[1,1])
    else:   
        eul1 = math.atan2(R[1,0],R[0,0])
        sp = math.sin(eul1)
        cp = math.cos(eul1)
        eul2 = math.atan2(-R[2,0],cp*R[0,0]+sp*R[1,0])
        eul3 = math.atan2(sp*R[0,2]-cp*R[1,2],cp*R[1,1]-sp*R[0,1])
    
    return (eul1, eul2, eul3)