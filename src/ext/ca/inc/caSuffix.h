#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined _WIN64
#define PLATFORM_DECORATION(f) f L"_64"
#elif defined ARM
#define PLATFORM_DECORATION(f) f L"_ARM"
#else
#define PLATFORM_DECORATION(f) f
#endif
