#!/usr/bin/env python3
"""
Simple singleton decorator implementation
"""

from typing import Type, TypeVar

T = TypeVar('T')


def singleton(cls: Type[T]) -> Type[T]:
    """
    Singleton decorator pattern
    Ensures only one instance of a class exists
    """
    instances = {}
    
    def get_instance(*args, **kwargs) -> T:
        if cls not in instances:
            instances[cls] = cls(*args, **kwargs)
        return instances[cls]
    
    return get_instance
