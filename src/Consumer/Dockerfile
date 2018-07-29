FROM microsoft/dotnet:2.1.300-sdk-alpine AS build

ARG BUILDCONFIG=RELEASE
ARG VERSION=1.0.0

# copy csproj and restore as distinct layers
COPY ./src/Consumer/Consumer.csproj ./Consumer/
COPY ./src/libs/Queueing/Queueing.csproj ./libs/Queueing/
RUN dotnet restore Consumer/Consumer.csproj

# copy everything else and build
COPY ./src/ .
WORKDIR /Consumer/
RUN dotnet publish -c $BUILDCONFIG -o out /p:Version=$VERSION

# build runtime image
FROM microsoft/dotnet:2.1-runtime-alpine
WORKDIR /app
COPY --from=build Consumer/out ./

ENTRYPOINT ["dotnet", "Consumer.dll"]