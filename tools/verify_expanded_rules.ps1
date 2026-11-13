$ErrorActionPreference = 'Stop'
$prRepository = Split-Path -Parent $PSScriptRoot
$prScripts = Join-Path $prRepository 'src/Assets/Scripts'
$prAssertions = @'
using System;
using System.Collections;
namespace NUnit.Framework {
    public class TestAttribute : Attribute { }
    public static class Assert {
        public static void IsTrue(bool v) { if(!v) throw new Exception("Expected true"); }
        public static void IsFalse(bool v) { if(v) throw new Exception("Expected false"); }
        public static void AreEqual(object a, object b) { if(!a.Equals(b)) throw new Exception("Expected " + a + ", got " + b); }
        public static void IsEmpty(ICollection a) { if(a.Count != 0) throw new Exception("Expected empty collection"); }
    }
    public static class CollectionAssert {
        public static void Contains(IEnumerable collection, object value) {
            foreach(object x in collection) if(x.Equals(value)) return;
            throw new Exception("Missing route cell");
        }
    }
}
'@
$prUnits = @($prAssertions,
    [IO.File]::ReadAllText((Join-Path $prScripts 'UXStealthState.cs')),
    [IO.File]::ReadAllText((Join-Path $prScripts 'UXGridPathfinder.cs')),
    [IO.File]::ReadAllText((Join-Path $prRepository 'src/Assets/Tests/EditMode/ExpandedStealthTests.cs')))
$prUsing = @($prUnits | ForEach-Object {
    [regex]::Matches($_, '(?m)^using [^;]+;') | ForEach-Object { $_.Value }
} | Select-Object -Unique) -join "`n"
$prBodies = @($prUnits | ForEach-Object { [regex]::Replace($_, '(?m)^using [^;]+;', '') })
Add-Type -TypeDefinition ($prUsing + "`n" + ($prBodies -join "`n")) -ErrorAction Stop
$prTests = [ExpandedStealthTests]::new()
foreach ($prMethod in [ExpandedStealthTests].GetMethods()) {
    if ($prMethod.GetCustomAttributes([NUnit.Framework.TestAttribute], $true).Length -gt 0) {
        $null = $prMethod.Invoke($prTests, @())
        Write-Output ('PASS ' + $prMethod.Name)
    }
}
