@echo off
echo ==============================================
echo  Generando FoldVision (Version Unico Archivo)
echo ==============================================

:: Limpiar la carpeta antigua de publicacion
if exist "publish\FoldVision_Abierta" (
    rmdir /s /q "publish\FoldVision_Abierta"
)

:: Compilar la aplicacion (versión abierta)
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false -o .\publish\FoldVision_Abierta

echo.
echo ==============================================
echo  Compilacion exitosa.
echo  Tu archivo esta en: publish\FoldVision_Unico_Archivo\
echo ==============================================
pause
