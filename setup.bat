@echo off
REM Check if Git is installed
where git >nul 2>nul
IF %ERRORLEVEL% NEQ 0 (
    echo Git is not installed. Please install Git.
    exit /b 1
)

REM Create a virtual environment in the current directory
echo Creating a virtual environment named "ml-agents-3.10-venv"...
py -3.10 -m venv ml-agents-3.10-venv

REM Activate the virtual environment
echo Activating the virtual environment...
call ml-agents-3.10-venv\Scripts\activate.bat

REM Upgrade pip to the latest version
echo Upgrading pip...
pip install --upgrade pip

REM Install PyTorch (CUDA version)
echo Installing PyTorch...
pip install torch~=2.2.1 --index-url https://download.pytorch.org/whl/cu121

REM Install the ml-agents package
echo Installing the ml-agents package...
pip install ml-agents

REM Install the ml-agents-envs package
echo Installing the ml-agents-envs package...
pip install ml-agents-envs

REM Inform the user that the setup is complete
echo Setup complete! The virtual environment "ml-agents-3.10-venv" has been created and activated.
echo The ml-agents and ml-agents-envs packages have been installed from the specified branch.
echo To activate the environment in the future, run: call ml-agents-3.10-venv\Scripts\activate.bat
echo To deactivate the environment, simply run: deactivate

pause
