@REM Clean up old build stuff
call clean.cmd

@REM Build Product A ver 1.0.0.0
pushd ProdAv100
call build.cmd
popd

@REM Build Product A ver 1.0.1.0 and Patch
pushd ProdAv101
call build.cmd
popd

@REM Build Product B ver 1.0.0.0
pushd ProdBv100
call build.cmd
popd

@REM Build Product B ver 1.0.1.0 and Patch
pushd ProdBv101
call build.cmd
popd

@REM Build Patch AB ver 1.0.1.1
pushd PatchABv101
call build.cmd
popd
