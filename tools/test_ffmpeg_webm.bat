@echo off
SET ROOT_DIR=%~dp0..\
PUSHD "%ROOT_DIR%"

SET OUTPUT_DIR=tools\tmp
SET OUTPUT=http://localhost:8080/publish/first?password=secret
SET OUTPUT_FILE=%OUTPUT_DIR%\test_ffmpeg_webm.webm
SET VIDEO=SCFF DirectShow Filter
SET AUDIO=Mixer (Realtek High Definition 
SET FFMPEG_EXE=ext\ffmpeg\x64\bin\ffmpeg.exe

MKDIR "%OUTPUT_DIR%"
DEL "%OUTPUT%"

REM [WEBM]
"%FFMPEG_EXE%" -rtbufsize 100MB -f dshow -framerate 30 -video_size 640x360 -pixel_format yuv420p -i video="%VIDEO%":audio="%AUDIO%" -g 69 -vcodec libvpx -preset medium -vb 700k -maxrate 700k -bufsize 1400k -crf 23 -qmin 10 -qmax 51 -acodec libvorbis -ar 48000 -ab 96k -ac 2 -vol 256 -threads 4 -metadata maxBitrate="700k" -f webm "%OUTPUT%"

POPD