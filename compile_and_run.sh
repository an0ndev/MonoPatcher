# source_files=$(find src -type f -printf '%p ' | sed 's/.$//')
source_files=$(find src -type f)
source_file_string=""

for source_file in $source_files
do
    source_file_string+="$source_file "
done
source_file_string=${source_file_string::-1}

dependency_files=$(find dep -type f)
dependency_file_string=""

for dependency_file in $dependency_files
do
    dependency_file_string+="-reference:$dependency_file "
done
dependency_file_string=${dependency_file_string::-1}

mcs $dependency_file_string $source_file_string -main:MonoPatcher -out:out/MonoPatcher.exe && out/MonoPatcher.exe $@
