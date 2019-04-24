# Python API for LGSVL Simulator

This folder contains Python API for LGSVL Simulator.

# Usage

Look into `quickstart` folder for simple usages.
To run these examples first start the simulator and leave it in main menu.
By default examples connect to Simulator on `localhost` address.
To change it, adjust first argument of `Simulator` constructor, or set up
`SIMULATOR_HOST` environment variable with hostname.

# Documentation

Documentation is available on our website: https://www.lgsvlsimulator.com/docs/python-api/

# Requirements

* Python 3.5 or higher

# Installing

    pip3 install --user .

    # install in development mode
    pip3 install --user -e .

# Running unit tests

    # run all unittests
    python3 -m unittest discover -v -c

    # run single test module
    python3 -m unittest -v -c tests/test_XXXX.py

    # run individual test case
    python3 -m unittest -v tests.test_XXX.TestCaseXXX.test_XXX
    python3 -m unittest -v tests.test_Simulator.TestSimulator.test_unload_scene

# Creating test coverage  report

    # (one time only) install coverage.py
    pip3 install --user coverage

    # run all tests with coverage
    ~/.local/bin/coverage run -m unittest discover

    # generate html report
    ~/.local/bin/coverage html --omit "~/.local/*","tests/*"

    # output is in htmlcov/index.html

# Copyright and License

Copyright (c) 2018 LG Electronics, Inc.

This software contains code licensed as described in LICENSE file in Simulator repository.
