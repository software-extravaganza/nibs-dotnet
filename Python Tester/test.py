import sys
from types import ModuleType
from cffi import FFI

ffi = FFI()
ffi.cdef("""
    int add(int a, int b);
""")

library_paths = {'../Library/bin/release/netcoreapp3.0/win-x64/native/Library.dll'}
for path in library_paths:
    try:
        lib = ffi.dlopen(path)
        break
    except OSError:
        pass
else:
    raise ImportError("No shared library could be loaded, "
                      "make sure that librtmp is installed.")

m1_name = "Module1"
m2_name = "Module2"
m1 = ModuleType(m1_name)
m1.go = "456"
m1.__dict__[m2_name] = ModuleType(m2_name)
def ex_method():
    return "example1_with " + Module1.go
m1.__dict__[m2_name].__dict__["TestMethod"] = ex_method
sys.modules[m1_name] = m1

Module1 = __import__(m1_name, fromlist=[''])
print(Module1.Module2.TestMethod())

answer = lib.add(3,5)
print(answer)