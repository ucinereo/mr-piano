import numpy as np


def make_homo(x: np.ndarray) -> np.ndarray:
    """
    Adds homogeneous coordinate to the array.

    :param x: (..., d) Input array

    :return: (..., d+1) Homogeneous array
    """

    return np.concatenate([x, np.ones((*x.shape[:-1], 1))], axis=-1)


def make_nohomo(x: np.ndarray) -> np.ndarray:
    """
    Normalizes a homogeneous array.

    :param x: (..., d+1) Input array

    :return: (..., d) Normalized inhomogeneous array
    """

    return x[..., :-1] / x[..., -1, None]
