FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
ARG SERVICE_NAME
COPY ./publish/${SERVICE_NAME}/ .
ENV RUN_DLL=${SERVICE_NAME}.dll
ENTRYPOINT ["sh", "-c", "dotnet $RUN_DLL"]
