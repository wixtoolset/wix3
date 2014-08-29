//-------------------------------------------------------------------------------------------------
// <copyright file="StrUtilTest.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

using namespace System;
using namespace System::Text;
using namespace System::Collections::Generic;
using namespace Xunit;

namespace CfgTests
{
	public ref class UriUtil
	{
	public:
		[Fact]
		void UriProtocolTest()
		{
			HRESULT hr = S_OK;

			LPCWSTR uri = L"a";
			URI_PROTOCOL uriProtocol = URI_PROTOCOL::URI_PROTOCOL_UNKNOWN;
			hr = UriProtocol(uri, &uriProtocol);
			ExitOnFailure(hr, "Failed to determine UriProtocol");
			Assert::Equal((int)URI_PROTOCOL::URI_PROTOCOL_UNKNOWN, (int)uriProtocol);

			uri = L"not a real scheme://localhost/";
			uriProtocol = URI_PROTOCOL::URI_PROTOCOL_UNKNOWN;
			hr = UriProtocol(uri, &uriProtocol);
			ExitOnFailure(hr, "Failed to determine UriProtocol");
			Assert::Equal((int)URI_PROTOCOL::URI_PROTOCOL_UNKNOWN, (int)uriProtocol);

			uri = L"https:/";
			uriProtocol = URI_PROTOCOL::URI_PROTOCOL_UNKNOWN;
			hr = UriProtocol(uri, &uriProtocol);
			ExitOnFailure(hr, "Failed to determine UriProtocol");
			Assert::Equal((int)URI_PROTOCOL::URI_PROTOCOL_UNKNOWN, (int)uriProtocol);

			uri = L"https://";
			uriProtocol = URI_PROTOCOL::URI_PROTOCOL_UNKNOWN;
			hr = UriProtocol(uri, &uriProtocol);
			ExitOnFailure(hr, "Failed to determine UriProtocol");
			Assert::Equal((int)URI_PROTOCOL::URI_PROTOCOL_HTTPS, (int)uriProtocol);

			uri = L"https://localhost/";
			uriProtocol = URI_PROTOCOL::URI_PROTOCOL_UNKNOWN;
			hr = UriProtocol(uri, &uriProtocol);
			ExitOnFailure(hr, "Failed to determine UriProtocol");
			Assert::Equal((int)URI_PROTOCOL::URI_PROTOCOL_HTTPS, (int)uriProtocol);

			uri = L"HTTPS://localhost/";
			uriProtocol = URI_PROTOCOL::URI_PROTOCOL_UNKNOWN;
			hr = UriProtocol(uri, &uriProtocol);
			ExitOnFailure(hr, "Failed to determine UriProtocol");
			Assert::Equal((int)URI_PROTOCOL::URI_PROTOCOL_HTTPS, (int)uriProtocol);

			uri = L"HtTpS://localhost/";
			uriProtocol = URI_PROTOCOL::URI_PROTOCOL_UNKNOWN;
			hr = UriProtocol(uri, &uriProtocol);
			ExitOnFailure(hr, "Failed to determine UriProtocol");
			Assert::Equal((int)URI_PROTOCOL::URI_PROTOCOL_HTTPS, (int)uriProtocol);

			uri = L"HTTP://localhost/";
			uriProtocol = URI_PROTOCOL::URI_PROTOCOL_UNKNOWN;
			hr = UriProtocol(uri, &uriProtocol);
			ExitOnFailure(hr, "Failed to determine UriProtocol");
			Assert::Equal((int)URI_PROTOCOL::URI_PROTOCOL_HTTP, (int)uriProtocol);

			uri = L"http:/";
			uriProtocol = URI_PROTOCOL::URI_PROTOCOL_UNKNOWN;
			hr = UriProtocol(uri, &uriProtocol);
			ExitOnFailure(hr, "Failed to determine UriProtocol");
			Assert::Equal((int)URI_PROTOCOL::URI_PROTOCOL_UNKNOWN, (int)uriProtocol);

			uri = L"http://localhost/";
			uriProtocol = URI_PROTOCOL::URI_PROTOCOL_UNKNOWN;
			hr = UriProtocol(uri, &uriProtocol);
			ExitOnFailure(hr, "Failed to determine UriProtocol");
			Assert::Equal((int)URI_PROTOCOL::URI_PROTOCOL_HTTP, (int)uriProtocol);

			uri = L"HtTp://localhost/";
			uriProtocol = URI_PROTOCOL::URI_PROTOCOL_UNKNOWN;
			hr = UriProtocol(uri, &uriProtocol);
			ExitOnFailure(hr, "Failed to determine UriProtocol");
			Assert::Equal((int)URI_PROTOCOL::URI_PROTOCOL_HTTP, (int)uriProtocol);

			uri = L"file://localhost/";
			uriProtocol = URI_PROTOCOL::URI_PROTOCOL_UNKNOWN;
			hr = UriProtocol(uri, &uriProtocol);
			ExitOnFailure(hr, "Failed to determine UriProtocol");
			Assert::Equal((int)URI_PROTOCOL::URI_PROTOCOL_FILE, (int)uriProtocol);

			uri = L"FILE://localhost/";
			uriProtocol = URI_PROTOCOL::URI_PROTOCOL_UNKNOWN;
			hr = UriProtocol(uri, &uriProtocol);
			ExitOnFailure(hr, "Failed to determine UriProtocol");
			Assert::Equal((int)URI_PROTOCOL::URI_PROTOCOL_FILE, (int)uriProtocol);

			uri = L"FiLe://localhost/";
			uriProtocol = URI_PROTOCOL::URI_PROTOCOL_UNKNOWN;
			hr = UriProtocol(uri, &uriProtocol);
			ExitOnFailure(hr, "Failed to determine UriProtocol");
			Assert::Equal((int)URI_PROTOCOL::URI_PROTOCOL_FILE, (int)uriProtocol);

			uri = L"FTP://localhost/";
			uriProtocol = URI_PROTOCOL::URI_PROTOCOL_UNKNOWN;
			hr = UriProtocol(uri, &uriProtocol);
			ExitOnFailure(hr, "Failed to determine UriProtocol");
			Assert::Equal((int)URI_PROTOCOL::URI_PROTOCOL_FTP, (int)uriProtocol);

			uri = L"ftp:/";
			uriProtocol = URI_PROTOCOL::URI_PROTOCOL_UNKNOWN;
			hr = UriProtocol(uri, &uriProtocol);
			ExitOnFailure(hr, "Failed to determine UriProtocol");
			Assert::Equal((int)URI_PROTOCOL::URI_PROTOCOL_UNKNOWN, (int)uriProtocol);

			uri = L"ftp://localhost/";
			uriProtocol = URI_PROTOCOL::URI_PROTOCOL_UNKNOWN;
			hr = UriProtocol(uri, &uriProtocol);
			ExitOnFailure(hr, "Failed to determine UriProtocol");
			Assert::Equal((int)URI_PROTOCOL::URI_PROTOCOL_FTP, (int)uriProtocol);

			uri = L"FtP://localhost/";
			uriProtocol = URI_PROTOCOL::URI_PROTOCOL_UNKNOWN;
			hr = UriProtocol(uri, &uriProtocol);
			ExitOnFailure(hr, "Failed to determine UriProtocol");
			Assert::Equal((int)URI_PROTOCOL::URI_PROTOCOL_FTP, (int)uriProtocol);

		LExit:
			;
		}
	};
}