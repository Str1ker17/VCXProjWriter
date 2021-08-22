#pragma once

/*
 * Preparations to enter full GCC mode
 */

// ensure we completely hid IDE identity
#undef _WIN32
#undef _MSC_VER
#undef _MSC_FULL_VER
#undef _MSC_BUILD
#undef _MSC_EXTENSIONS

// for design-time purposes it may be useful to know whether we use MSVS
#define VCXPROJWRITER

/*
 * Compatibility problems, extensions, etc.
 */

#define __PRETTY_FUNCTION__ __func__
#define __extension__
#define __asm__
#define __asm__(...)
#define __asm
#define __asm(...)
#define asm
#define asm(...)
#define volatile(...)
#define __volatile__(...)

/*
 * Builtins and so on
 */

//
// Simplified but syntactically identical
//
#define __atomic_load_n(ptr, memorder) (*(ptr))
#define __atomic_store_n(ptr, val, memorder) (*(ptr) = val)

//
// Stubs; for example, dependent on typeof which still not supported
//
#define __builtin_offsetof(type, member) 0
