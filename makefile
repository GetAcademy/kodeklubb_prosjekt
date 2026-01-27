.PHONY: clean build setup-tests

new-build:  clean build
ci-pipeline: clean sln-generate build test-flow

clean: #	Clean up build artifacts
	@echo "Cleaning up build artifacts..."
	find . -depth -type d \( -name "bin" -o -name "obj" -o -name "TestResults"  \) -exec rm -rf {} +;
	clear
	@echo "build artifacts Cleaned"

build: #	Builds the entire solution
	@echo "Restoring Dependencies..."
	dotnet restore codeClub.API.csproj

	@echo "Building solution..."
	dotnet build codeClub.sln
	clear
	@echo "Solution built successfully"

sln-generate: #	Cleans up Solution file
	@echo "Generating solution files..."
	# Command to generate solution files goes here
	rm -rf *.sln
	dotnet new sln -n codeClub
	dotnet sln codeClub.sln add codeClub.API.csproj

	dotnet clean Intranet.sln
	dotnet restore Intranet.sln
	dotnet build Intranet.sln
	clear
	@echo "Solution files generated"
