// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Runtime.InteropServices;

namespace SouXiaoAVE.Yara;

internal static class YaraNative
{
    private const String YaraDll = "libyara64.dll";

    public const Int32 ERROR_SUCCESS = 0;
    public const Int32 ERROR_INSUFFICIENT_MEMORY = 1;
    public const Int32 ERROR_COULD_NOT_ATTACH_TO_PROCESS = 2;
    public const Int32 ERROR_COULD_NOT_OPEN_FILE = 3;
    public const Int32 ERROR_COULD_NOT_MAP_FILE = 4;
    public const Int32 ERROR_INVALID_FILE = 5;
    public const Int32 ERROR_CORRUPT_FILE = 6;
    public const Int32 ERROR_UNSUPPORTED_FILE_VERSION = 7;
    public const Int32 ERROR_INVALID_REGULAR_EXPRESSION = 8;
    public const Int32 ERROR_INVALID_HEX_STRING = 9;
    public const Int32 ERROR_SYNTAX_ERROR = 10;
    public const Int32 ERROR_LOOP_NESTING_LIMIT_EXCEEDED = 11;
    public const Int32 ERROR_DUPLICATED_LOOP_IDENTIFIER = 12;
    public const Int32 ERROR_DUPLICATED_IDENTIFIER = 13;
    public const Int32 ERROR_DUPLICATED_TAG_IDENTIFIER = 14;
    public const Int32 ERROR_DUPLICATED_META_IDENTIFIER = 15;
    public const Int32 ERROR_DUPLICATED_STRING_IDENTIFIER = 16;
    public const Int32 ERROR_UNREFERENCED_STRING = 17;
    public const Int32 ERROR_UNDEFINED_STRING = 18;
    public const Int32 ERROR_UNDEFINED_IDENTIFIER = 19;
    public const Int32 ERROR_MISPLACED_ANONYMOUS_STRING = 20;
    public const Int32 ERROR_INCLUDES_CIRCULAR_REFERENCE = 21;
    public const Int32 ERROR_INCLUDE_DEPTH_EXCEEDED = 22;
    public const Int32 ERROR_WRONG_TYPE = 23;
    public const Int32 ERROR_EXEC_STACK_OVERFLOW = 24;
    public const Int32 ERROR_SCAN_TIMEOUT = 25;
    public const Int32 ERROR_TOO_MANY_SCAN_THREADS = 26;
    public const Int32 ERROR_CALLBACK_ERROR = 27;
    public const Int32 ERROR_INVALID_ARGUMENT = 28;
    public const Int32 ERROR_TOO_MANY_MATCHES = 29;
    public const Int32 ERROR_INTERNAL_FATAL_ERROR = 30;
    public const Int32 ERROR_NESTED_FOR_OF_LOOP = 31;
    public const Int32 ERROR_INVALID_FIELD_NAME = 32;
    public const Int32 ERROR_UNKNOWN_MODULE = 33;
    public const Int32 ERROR_NOT_A_STRUCTURE = 34;
    public const Int32 ERROR_NOT_AN_ARRAY = 35;
    public const Int32 ERROR_NOT_A_FUNCTION = 36;
    public const Int32 ERROR_INVALID_FORMAT = 37;
    public const Int32 ERROR_TOO_MANY_ARGUMENTS = 38;
    public const Int32 ERROR_WRONG_ARGUMENTS = 39;
    public const Int32 ERROR_WRONG_RETURN_TYPE = 40;

    public const Int32 CALLBACK_MSG_RULE_MATCHING = 1;
    public const Int32 CALLBACK_MSG_RULE_NOT_MATCHING = 2;
    public const Int32 CALLBACK_MSG_TOO_MANY_MATCHES = 3;
    public const Int32 CALLBACK_MSG_CONSOLE_LOG = 4;

    public const Int32 CALLBACK_CONTINUE = 0;
    public const Int32 CALLBACK_ABORT = 1;
    public const Int32 CALLBACK_ERROR = 2;

    public const Int32 SCAN_FLAGS_FAST_MODE = 1;
    public const Int32 SCAN_FLAGS_PROCESS_MEMORY = 2;
    public const Int32 SCAN_FLAGS_NO_TRYCATCH = 4;
    public const Int32 SCAN_FLAGS_REPORT_RULES_MATCHING = 8;
    public const Int32 SCAN_FLAGS_REPORT_RULES_NOT_MATCHING = 16;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate Int32 YrCallbackFunc(IntPtr context, Int32 message, IntPtr rule, IntPtr data);

    [DllImport(YaraDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern Int32 yr_initialize();

    [DllImport(YaraDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern Int32 yr_finalize();

    [DllImport(YaraDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void yr_finalize_thread();

    [DllImport(YaraDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern Int32 yr_compiler_create(out IntPtr compiler);

    [DllImport(YaraDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void yr_compiler_destroy(IntPtr compiler);

    [DllImport(YaraDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern Int32 yr_compiler_add_file(IntPtr compiler, IntPtr file, IntPtr namespace_, String file_path);

    [DllImport(YaraDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern Int32 yr_compiler_add_string(IntPtr compiler, String rulesString, IntPtr namespace_);

    [DllImport(YaraDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern Int32 yr_compiler_add_fd(IntPtr compiler, IntPtr fd, IntPtr namespace_, String file_path);

    [DllImport(YaraDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern Int32 yr_compiler_get_rules(IntPtr compiler, out IntPtr rules);

    [DllImport(YaraDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void yr_compiler_set_callback(IntPtr compiler, IntPtr callback, IntPtr userData);

    [DllImport(YaraDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern Int32 yr_rules_create(out IntPtr rules);

    [DllImport(YaraDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void yr_rules_destroy(IntPtr rules);

    [DllImport(YaraDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern Int32 yr_rules_load(String filename, out IntPtr rules);

    [DllImport(YaraDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern Int32 yr_rules_save(IntPtr rules, String filename);

    [DllImport(YaraDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern Int32 yr_rules_scan_file(IntPtr rules, String filename, Int32 flags, YrCallbackFunc callback, IntPtr userData, Int32 timeout);

    [DllImport(YaraDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern Int32 yr_rules_scan_mem(IntPtr rules, Byte[] buffer, UIntPtr length, Int32 flags, YrCallbackFunc callback, IntPtr userData, Int32 timeout);

    [DllImport(YaraDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr yr_rule_tags(IntPtr rule);

    [DllImport(YaraDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr yr_rule_metas(IntPtr rule);

    [DllImport(YaraDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr yr_rule_strings(IntPtr rule);

    [DllImport(YaraDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr yr_rule_namespace(IntPtr rule);

    [DllImport(YaraDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern Int32 yr_rule_get_string(IntPtr rule, Int32 stringIdx, out IntPtr stringPtr);

    [DllImport(YaraDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr yr_get_error_message();

    [DllImport(YaraDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern Int32 yr_get_last_error();

    [DllImport(YaraDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr yr_rule_identifier(IntPtr rule);
}
