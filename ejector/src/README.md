# Ejector unit operation — C# engine and CAPE-OPEN adapter

| Project | Target | Contents |
|---|---|---|
| `EjectorCore` | netstandard2.0 | Calculation engine: port of `../prototype/ejector1d.py` (`Rate`, `RateFlows`, `Design`, `DesignForLift`); thermodynamics only through `IFlash` |
| `EjectorCore.Tests` | net8.0 | Engine checks on an ideal gas, and regression against the Python reference on identical CoolProp thermodynamics |
| `Ejector.CapeOpen` | net8.0-windows, x64, comhost | CAPE-OPEN 1.0 unit (`Ejector.EjectorUnitOperation.1`): ports Motive, Suction, Outlet; `PmeFlash` runs PH/PS/PT/PQ flashes on a scratch material object of the PME's property package |
| `Ejector.CapeOpen.Tests` | net8.0-windows | The unit headless, through a mock material object that performs real flashes (CoolProp or ideal gas) |

The technical reference is in `../techref` (Sphinx). The CAPE-OPEN interface (modes, parameters, errors) is
documented in its appendix.

## Build and test

```
dotnet test EjectorUnitOp.sln -c Release
```

The CoolProp-based tests need the CoolProp shared library (`CoolProp.dll`, x64). Point `COOLPROP_DLL` at it,
or keep a CoolProp checkout with `build_shared/Release/CoolProp.dll` next to this repository. Without the
DLL those tests are skipped. The reference results come from `../prototype/export_regression.py`; re-run it
after any change to the Python model.

## Register (per user, no admin)

After a Release build, from `Ejector.CapeOpen/bin/x64/Release/net8.0-windows/win-x64/`:

```
powershell -ExecutionPolicy Bypass -File register-user.ps1
```

`unregister-user.ps1` removes the registration; `register.bat` registers per machine. The .NET 8 Desktop
runtime is required. Set `EJECTOR_CAPEOPEN_LOG` to write `ejector_capeopen.log` next to the DLL.

Status: tested headless only; not yet run inside COFE or DWSIM.
