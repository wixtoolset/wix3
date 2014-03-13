#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scacost.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Costs for various server custom actions.
// </summary>
//-------------------------------------------------------------------------------------------------

const UINT COST_IIS_TRANSACTIONS = 10000;

const UINT COST_IIS_CREATEKEY = 5000;
const UINT COST_IIS_DELETEKEY = 5000;
const UINT COST_IIS_WRITEVALUE = 5000;
const UINT COST_IIS_DELETEVALUE = 5000;
const UINT COST_IIS_CREATEAPP = 5000;
const UINT COST_IIS_DELETEAPP = 5000;

const UINT COST_SQL_CREATEDB = 10000;
const UINT COST_SQL_DROPDB = 5000;
const UINT COST_SQL_CONNECTDB = 5000;
const UINT COST_SQL_STRING = 5000;

const UINT COST_PERFMON_REGISTER = 1000;
const UINT COST_PERFMON_UNREGISTER = 1000;

const UINT COST_SMB_CREATESMB = 10000;
const UINT COST_SMB_DROPSMB = 5000;

const UINT COST_CERT_ADD = 5000;
const UINT COST_CERT_DELETE = 5000;

const UINT COST_USER_ADD = 10000;
const UINT COST_USER_DELETE = 10000;

const UINT COST_PERFMONMANIFEST_REGISTER = 1000;
const UINT COST_PERFMONMANIFEST_UNREGISTER = 1000;

const UINT COST_EVENTMANIFEST_REGISTER = 1000;
const UINT COST_EVENTMANIFEST_UNREGISTER = 1000;

