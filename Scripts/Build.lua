-- Solution configuration
solution "GameModifiers"
    configurations { "Debug", "Release" }
    platforms { "Any CPU" }
	location "../"

-- Project configuration
project "GameModifiers"
    kind "SharedLib"
    language "C#"
    dotnetframework "net8.0"
	nuget { "CounterStrikeSharp.API:1.0.339", "Dapper:2.1.66", "Microsoft.CSharp:4.7.0" }
    namespace "GameModifiers"
	targetdir "../Binaries/%{cfg.buildcfg}"
	location "../Build/GameModifiers"

    files { "../Source/**.cs" }

    vsprops {
		Nullable = "enable",
		GenerateAssemblyInfo = "false",
        CopyLocalLockFileAssemblies = "true",
		AllowUnsafeBlocks = "true"
    }

    filter "configurations:Debug"
        symbols "On"

    filter "configurations:Release"
        optimize "On"