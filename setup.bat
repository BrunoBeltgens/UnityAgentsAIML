@echo off
where git >nul 2>nul
IF %ERRORLEVEL% NEQ 0 (
    echo Git is not installed. Please install Git.
    pause
    exit /b 1
)

py -3.10 --version >nul 2>nul
IF %ERRORLEVEL% NEQ 0 (
    echo Python 3.10 is not installed. Please install Python 3.10 from https://www.python.org/downloads/release/python-31011/.
    pause
    exit /b 1
)

echo Creating a virtual environment named ".venv"...
py -3.10 -m venv .venv

echo Activating the virtual environment...
call .venv\Scripts\activate.bat

echo Upgrading pip to the latest version...
python.exe -m pip install --upgrade pip

echo Installing packages...
pip install torch -f https://download.pytorch.org/whl/torch_stable.html
pip install ./ml-agents-envs
pip install ./ml-agents

echo Setup complete! The virtual environment ".venv" has been created and activated.
echo To activate the environment in the future, run: .venv\Scripts\activate.bat
echo To deactivate the environment, run: .venv\Scripts\deactivate.bat

pause
