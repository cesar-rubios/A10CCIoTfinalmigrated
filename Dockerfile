# 1. Imagen base: última versión de Ubuntu
FROM ubuntu:latest

# 2. Instala dependencias necesarias
RUN apt-get update && apt-get install -y wget apt-transport-https git sudo

# 3. Instala .NET SDK (ajusta la versión si hace falta)
RUN sudo apt update && sudo apt upgrade -y
RUN wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
RUN sudo dpkg -i packages-microsoft-prod.deb
RUN sudo apt update
RUN sudo apt install -y apt-transport-https
RUN sudo apt install -y dotnet-sdk-8.0 

# 4. Copia los scripts de inicio si corresponde (como entrypoint.sh)
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]