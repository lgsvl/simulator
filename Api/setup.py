#!/usr/bin/env python3

from setuptools import setup

setup(
    name="lgsvl",
    description="LGSVL Simulator Api",
    author="LGSVL",
    author_email="contact@lgsvlsimulator.com",
    python_requires=">=3.5.0",
    url="https://github.com/lgsvl/simulator",
    packages=["lgsvl"],
    install_requires=["websockets>=7.0"],
    license="Other",
    classifiers=[
        "License :: Other/Proprietary License",
        "Programming Language :: Python :: 3",
        "Programming Language :: Python :: 3.5",
    ],
)
