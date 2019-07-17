# How to generate protobuf from Apollo
You can follow the documentation to generate new protobuf files. Link [issue](https://github.com/lgsvl/simulator/issues/207)  

## Protobuf-net
We use "Protobuf-net" to generate protobuf for lgsvl. Lgsvl uses Unity, which is developed using C#. Currently Google's protobuf does not support C# official. At present we can use the "Protobuf-net" to solve this problem.  
You can download the "Protobuf-net" and build a version [here](https://github.com/protobuf-net/protobuf-net), Or you can download the release version. Protobuf-net need windows visual studio to build.

You need modify the float issue in "src/protobuf-net.Reflection/CSharpCodeGenerator.cs:375", and then build a version. Link [issue](https://github.com/protobuf-net/protobuf-net/issues/452)  
```
                    switch (defaultValue)
                    {
                        case "inf": defaultValue = "float.PositiveInfinity"; break;
                        case "-inf": defaultValue = "float.NegativeInfinity"; break;
                        case "nan": defaultValue = "float.NaN"; break;
                        default:
                            if (!string.IsNullOrEmpty(defaultValue) &&
                                float.TryParse(defaultValue, out var f) &&
                                !defaultValue.EndsWith("f"))
                            {
                                defaultValue += "f";
                            }
                            break;
                    }
```

## Apollo
1. You need collect all the protobuf files into a file. Below script use to generate them. Save the script name as "proto.sh" and cd into "apollo" folder. Then run the command and you will get a folder named "csharp_proto" containing all the ".proto" files.  
```
#!/usr/bin/env bash
mkdir csharp_proto
find modules/ cyber/ -name "*.proto" \
    | grep -v node_modules \
    | xargs -i cp --parent {} csharp_proto

cd csharp_proto
mkdir gen
find modules/ -type d | xargs -i mkdir -p gen/{}
```

2. Generate the compile script. Use the following script to generate a compile script to "Protobuf-net".
```
find modules/ cyber/ -name "*.proto" \
    | grep -v node_modules \
    | xargs -i echo protogen.exe --proto_path=../net462 --csharp_out=gen +names=original +langver=3 {}  > run.bat
```

3. Completion the namespace 
Link [issue](https://stackoverflow.com/questions/56695946/can-protobuf-package-be-omitted-when-using/56697113#56697113), a remedy is to make these additions eg: "common.Point3D" to "apollo.common.Point3D". These errors will only appear at compile time, so after reviewing the error after compilation, you can skip it now.   

## Generate protobuf
Copy the "csharp_proto" folder to "Protobuf-net" executable folder, Then run the "run.bat". The generate ".cs" files is in "gen" folder.

## Use the protobuf
Copy the gen protobuf to "lgsvl/simulator_master/simulator/Assets/Scripts/Cyber/Protobuf". If you found some error, maybe some protobuf conflict with lgsvl, then do with it or submit a issue.
