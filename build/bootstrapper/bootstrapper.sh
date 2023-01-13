#!/bin/bash +x

echo "🥾 ---- Running bootstrapper ---- 🥾"

#dotnet-script

if dotnet tool list -g | grep dotnet-script > /dev/null ; then
   echo "✅ dotnet-script was found"
else
   echo "❌ dotnet-script was not found, installing..."
   dotnet tool install -g dotnet-script > /dev/null
   CheckIfInstalledCorrectly "dotnet-script"
fi

if npm list -g | grep style-dictionary > /dev/null ; then
   echo "✅ npm package: style-dictionary was found"
else
   npm install style-dictionary
fi
