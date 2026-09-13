@echo off
echo ==============================================
echo  Generando FoldVision
echo ==============================================

:: Limpiar la carpeta antigua de publicacion
if exist "publish\FoldVision" (
    rmdir /s /q "publish\FoldVision"
)

:: Compilar la aplicacion (Para Instalador)
dotnet publish -c Release -r win-x64 --self-contained false -o .\publish\FoldVision

:: Compilar la aplicacion Portable (Unico Archivo)
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o .\publish\Portable
move /y ".\publish\Portable\FoldVision.exe" ".\publish\FoldVision_Portable.exe" >nul
rmdir /s /q ".\publish\Portable"

echo.
echo ==============================================
echo  Compilacion exitosa.
echo  Archivos base para instalador: publish\FoldVision\
echo  Ejecutable Portable:           publish\FoldVision_Portable.exe
echo ==============================================
echo.

:: Compilar el instalador con Inno Setup si esta instalado
set "INNO_SETUP=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if exist "%INNO_SETUP%" (
    echo ==============================================
    echo  Generando Instalador con Inno Setup...
    echo ==============================================
    "%INNO_SETUP%" installer.iss
    echo.
    echo  El instalador se genero correctamente en la carpeta Output\
) else (
    echo ==============================================
    echo  No se encontro Inno Setup 6 instalado.
    echo  Generacion del instalador omitida.
    echo ==============================================
)
echo.
pause
