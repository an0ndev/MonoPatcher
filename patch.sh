set -e # exits if non-zero return value
set -o pipefail # exits if non-zero return value in command with piped stderr/out
./compile_and_run.sh in/Assembly-CSharp.dll patches Assembly-CSharp.dll
