@echo off
echo ==============================================
echo  Generando FoldVision (Version Unico Archivo)
echo ==============================================

:: Limpiar la carpeta antigua de publicacion
if exist "publish\FoldVision_Unico_Archivo" (
    rmdir /s /q "publish\FoldVision_Unico_Archivo"
)

:: Compilar la aplicacion en un unico archivo
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o .\publish\FoldVision_Unico_Archivo

echo.
echo ==============================================
echo  Compilacion exitosa.
echo  Tu archivo esta en: publish\FoldVision_Unico_Archivo\
echo ==============================================
pause
