set -e # exits if non-zero return value
set -o pipefail # exits if non-zero return value in command with piped stderr/out
./compile_and_run.sh in/Assembly-CSharp.dll patches Assembly-CSharp.dll
cp Assembly-CSharp.dll /fast/Work/Steam\ Games/steamapps/common/Ravenfield/ravenfield_Data/Managed/
/fast/Work/Steam\ Games/steamapps/common/Ravenfield/ravenfield.x86_64
