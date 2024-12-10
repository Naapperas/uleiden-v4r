SHELL := /bin/bash

all:
	@source ./bin/activate
	@pip install -r requirements.txt
	python src/process.py
	@deactivate
	@exit

format:
	@source ./bin/activate
	@pip install -r requirements.txt
	black .
	@deactivate
	@exit