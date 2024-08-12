#!/bin/bash +x

echo "🥾 ---- Running bootstrapper ---- 🥾"

#dotnet-script

if dotnet tool list -g | grep dotnet-script > /dev/null ; then
   echo "✅ dotnet-script was found"
else
   echo "❌ dotnet-script was not found, installing..."
   dotnet tool install -g dotnet-script > /dev/null
fi

if npm list --prefix ./src | grep style-dictionary > /dev/null ; then
   echo "✅ npm package: style-dictionary was found"
   echo "style-dictionary version: $(npm view style-dictionary version)"
else
   npm install --prefix ./src style-dictionary --silent
   echo "style-dictionary version: $(npm view style-dictionary version)"
fi
