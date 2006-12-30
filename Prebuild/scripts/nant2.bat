@rem Generates a .build files 
@rem for NAnt
cd ..
Prebuild /target nant /file prebuild.xml /build NET_2_0 /pause
