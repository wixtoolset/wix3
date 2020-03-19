#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(_M_ARM64)
#define PLATFORM_DECORATION(f) f L"_A64"
#elif defined(_M_AMD64)
#define PLATFORM_DECORATION(f) f L"_64"
#elif defined(_M_ARM)
#define PLATFORM_DECORATION(f) f L"_ARM"
#else
#define PLATFORM_DECORATION(f) f
#endif
