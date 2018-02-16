#!/bin/bash
mcs -t:library server.cs -out:cc.dll
mcs main.cs -r:cc.dll -out:host.exe
