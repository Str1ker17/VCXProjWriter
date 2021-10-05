/* Get the newest version from https://gist.github.com/Str1ker17/3addcd2c768bc96fd7c1487594eae582 */

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

#define thread_local
#define restrict

#define __extension__
#define __attribute__(...)
#define __attribute(...)

#define asm __asm__
#define __asm __asm__

//#define __typeof__(a) int
//#define __typeof(a) __typeof__(a)
//#define typeof(a) __typeof__(a)

/*
 * Builtins and so on
 */

//
// Simplified but syntactically identical
//
#define __atomic_load_n(ptr, memorder) (*(ptr))
#define __atomic_store_n(ptr, val, memorder) (*(ptr) = (val))

#define __sync_fetch_and_add(ptr, amount) (*(ptr) + (amount))
#define __sync_fetch_and_and(ptr, amount) (*(ptr) & (amount))

#define __builtin_expect(exp, likely) (exp)
#define __builtin_bswap16(value) ((((value) & 0xff) << 8) | (((value) >> 8) & 0xff))
#define __builtin_bswap32(value) ((__builtin_bswap16((value) & 0xffff) << 16) | __builtin_bswap16((value) >> 16))
#define __builtin_bswap64(value) ((__builtin_bswap32((value) & 0xffffffff) << 32) | __builtin_bswap32((value) >> 32))

#define __builtin_offsetof(t, m) ((__INTPTR_TYPE__)(&(((t*)0)->m)))

#define __builtin_choose_expr(const_exp, exp1, exp2) ((const_exp) ? (exp1) : (exp2))

//
// Stubs; for example, dependent on typeof which still not supported. Or just lazy :)
//
#define __builtin_types_compatible_p(t1, t2) 1

#define __builtin_return_address(n) 0
#define __builtin_alloca(size) ((void*)(__SIZEOF_PTRDIFF_T__ + (size)))

#define __builtin_add_overflow(a, b, res) ((*(res)) = (a) + (b))
#define __builtin_sub_overflow(a, b, res) ((*(res)) = (a) + (b))
#define __builtin_mul_overflow(a, b, res) ((*(res)) = (a) * (b))
#define __builtin_add_overflow_p(a, b, c) 0
#define __builtin_sub_overflow_p(a, b, c) 0
#define __builtin_mul_overflow_p(a, b, c) 0

#define __builtin_clz(value) 0
#define __builtin_ctz(value) 0
#define __builtin_ctzl(value) 0
#define __builtin_ctzll(value) 0
#define __builtin_ffs(value) 0
#define __builtin_ffs_l(value) 0
#define __builtin_ffs_ll(value) 0

#define __builtin_memcpy(dst, src, size) (dst)
#define __builtin_memmove(dst, src, size) (dst)
#define __builtin_memset(dst, val, size)

#define __builtin_parity(value) 0

#define __sync_fetch_and_or(ptr, mask) (*(ptr))
#define __sync_val_compare_and_swap(val, old, new) (*(val))

#define __sync_synchronize()
#define __sync_lock_release(lock)

#define __builtin_trap()

/*
 * User defined
 */
