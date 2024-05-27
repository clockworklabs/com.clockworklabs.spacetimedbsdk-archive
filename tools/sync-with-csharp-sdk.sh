#!/bin/bash

cd "$(dirname "$0")"

source fix-meta-files.bash

STDB_CSHARP="../../spacetimedb-csharp-sdk"

if ! [ -d "../../spacetimedb-csharp-sdk" ] ; then
    echo "Please clone the spacetimedb-csharp-sdk as a directory sibiling of the UnitySDK repo."
    echo "git clone https://github.com/clockworklabs/spacetimedb-csharp-sdk.git"
    exit 1
fi

if ! which rsync > /dev/null ; then
    echo "This script requires rsync, which may be installed through brew on macOS:"
    echo ""
    echo "  brew install rsync"
    echo ""
    echo "or through your package manager in Linux."
fi

rsync -av $STDB_CSHARP/src/ ../Scripts
fix_meta_files
