#!/bin/bash
printenv

printenv

sed -i.bak "s/AndroidBundleVersionCode: 8/AndroidBundleVersionCode: $UCB_BUILD_NUMBER/g" ProjectSettings/ProjectSettings.asset

sed -i.bak "s/iPhone: 0/iPhone: $UCB_BUILD_NUMBER/g" ProjectSettings/ProjectSettings.asset
