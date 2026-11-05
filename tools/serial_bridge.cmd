@echo off
rem Double-click to connect a USB micro:bit to the Unity Editor (Play mode). See serial_bridge.ps1.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0serial_bridge.ps1" %*
if errorlevel 1 pause
