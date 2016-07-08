---
title: Preprocessor
layout: documentation
after: tools
---
# Preprocessor

Often you will need to add different pieces of your setup during build time depending on many factors such as the SKU being built. This is done by using conditional statements that will filter the xml before it is sent to the WiX compiler (candle). If the statement evaluates to true, the block of xml will be sent to candle. If the statement evaluates to false, candle will never see that section of xml.

The conditional statements are Boolean expressions based on environment variables, variables defined in the xml, literal values, and more.

### Example

Let&rsquo;s start with an example. Say you want to include a file if you&rsquo;re building the &ldquo;Enterprise SKU.&rdquo; Your build uses an environment variable `%MySku%=Enterprise` to specify this sku.

When you build the enterprise sku, this file will be included in the xml passed on to candle. When you build a different sku, the xml from EnterpriseFeature.wxi will be ignored.

    <?if $(env.MySku) = Enterprise ?>
      <?include EnterpriseFeature.wxi ?>
    <?endif ?>

## Include Files &lt;?include?&gt;

As shown in the example above, files can be included by using the include tag. The filename referenced in the tag will be processed as if it were part of this file.

The root element of the include file must be &lt;Include&gt;. There are no other requirements beyond the expected wix schema. For example,

    <Include>
       <Feature Id='MyFeature' Title='My 1st Feature' Level='1'>
          <ComponentRef Id='MyComponent' />
       </Feature>
    </Include>

## Variables

Any variable can be tested for its value or simply its existence. Custom variables can also be defined in your xml.

Three types of variables are supported:

<dl>
  <dt>$(env._NtPostBld)</dt>
  <dd>Gets the environment variable %_NtPostBld%</dd>
  <dt>$(sys.CURRENTDIR)</dt>
  <dd>Gets the system variable for the current directory</dd>
  <dt>$(var.A)</dt>
  <dd>Gets the variable A that was defined in this xml</dd>
</dl>

The preprocessor evaluates variables throughout the entire document, including in &lt;?if?&gt; expressions and attribute values.

### Environment Variables

Any environment variable can be referenced with the syntax $(env.VarName). For example, if you want to retrieve the environment variable %\_BuildArch%, you would use $(env.\_BuildArch). Environment variable names are case-insensitive.

### System Variables

WiX has some built-in variables. They are referenced with the syntax $(sys.VARNAME) and are always in upper case.

<dl>
  <dt>CURRENTDIR<dt>
  <dd>The current directory where the build process is running.</dd>
  <dt>SOURCEFILEPATH</dt>
  <dd>The full path to the file being processed.</dd>
  <dt>SOURCEFILEDIR</dt>
  <dd>The directory containing the file being processed.</dd>
  <dt>BUILDARCH</dt> 
  <dd>The platform (Intel, x64, Intel64, ARM) this package is compiled for (set by the -arch switch to Candle.exe or the InstallerPlatform MSBuild property).</dd>
</dl>

NOTE: All built-in directory variables are &ldquo;\&rdquo; terminated.

### Custom variables &lt;? define ?&gt;

If you want to define custom variables, you can use the &lt;?define?&gt; statement. You can also define variables on the command line using candle.exe using the -d switch. Later, the variables are referred to in the &lt;?if?&gt; statements with the syntax $(var.VarName). Variable names are case-sensitive.

How to define the existence of a variable:  
&lt;?define MyVariable ?&gt;  

How to define the value of a variable (<i>note: quotes are required if the value or the expansion of other variables in the value contain spaces</i>):  
&lt;?define MyVariable = &ldquo;Hello World&rdquo; ?&gt;  
&lt;?define MyVariable = &ldquo;$(var.otherVariableContainingSpaces)&rdquo; ?&gt;  

The right side of the definition can also refer to another variable:  
&lt;?define MyVariable = $(var.BuildPath)\x86\bin\ ?&gt;

How to undefine a variable:  
&lt;?undef MyVariable ?&gt;  

To define variables on the command line, you can type a command similar to the following:

    candle.exe -dMyVariable="Hello World" ...

You can refer to variables in your source that are defined only on the command line, but candle.exe will err when preprocessing your source code if you do not define those variables on the command line.

## Conditional Statements

There are several conditional statements, they include:

* &lt;?if ?&gt;
* &lt;?ifdef ?&gt;
* &lt;?ifndef ?&gt;
* &lt;?else?&gt;
* &lt;?elseif ?&gt;
* &lt;?endif?&gt;

The purpose of the conditional statement is to allow you to include or exclude a segment of xml at build time. If the expression evaluates to true, it will be included. If it evaluates to false, it will be ignored.

The conditional statements always begin with either the &lt;?if ?&gt;, &lt;?ifdef ?&gt;, or &lt;?ifndef ?&gt; tags. They are followed by an xml block, an optional &lt;?else?&gt; or &lt;?elseif ?&gt; tag, and must end with an &lt;?endif?&gt; tag.

### Expressions (used in &lt;?if ?&gt; and &lt;?elseif ?&gt;)

For example: &lt;?if [expression]?&gt;

The expression found inside the &lt;?if ?&gt; and &lt;?elseif ?&gt; tags is a Boolean expression. It adheres to a simple grammar that follows these rules:

<ul>
  <li>The expression is evaluated left to right</li>
  <li>Expressions are case-sensitive with the following exceptions:</li>
  <li style="list-style: none; display: inline">
    <ul>
      <li>Environmental variable names</li>
      <li>These keywords: and, or, not</li>
      <li>The ~= operator is case-insensitive.</li>
    </ul>
  </li>
  <li>All variables must use the $() syntax or else they will be considered a literal value.</li>
  <li>If you want to use a literal $(, escape the dollar sign with a second one. For example, $$(</li>
  <li>Variables can be compared to a literal or another variable</li>
  <li style="list-style: none; display: inline">
    <ul>
      <li>Comparisons with =, !=, and ~= are string comparisons.</li>
      <li>Comparisons with inequality operators (&lt;, &lt;=, &gt;, &gt;=) must be done on integers.</li>
      <li>If the variable doesn't exist, evaluation will fail and an error will be raised.</li>
    </ul>
  </li>
  <li>The operator precedence is as follows. Note that &ldquo;and&rdquo; and &ldquo;or&rdquo; have the same precedence:</li>
  <li style="list-style: none; display: inline">
    <ul>
      <li>""</li>
      <li>(), $( )</li>
      <li>&lt;, &gt;, &lt;=, &gt;=, =, !=, ~=</li>
      <li>Not</li>
      <li>And, Or</li>
    </ul>
  </li>
  <li>Nested parenthesis are allowed.</li>
  <li>Literals can be surrounded by quotes, although quotes are not required.</li>
  <li>Quotes, leading, and trailing white space are stripped off literal values.</li>
  <li>Invalid expressions will cause an exception to be thrown.</li>
</ul>

### Variables (used in &lt;ifdef ?&gt; and &lt;ifndef ?&gt;)

For example: &lt;?ifdef [variable] ?&gt;

For &lt;ifdef ?&gt;, if the variable has been defined, this statement will be true. &lt;ifndef ?&gt; works in the exact opposite way.

### More Examples

Note that these examples will actually each be a no-op because there aren&rsquo;t any tags between the if and endif tags.

<pre>
    &lt;?define myValue  = "3"?&gt;
    &lt;?define system32=$(env.windir)\system32  ?&gt;
    &lt;?define B = "good var" ?&gt;
    &lt;?define C =3 ?&gt;
    &lt;?define IExist ?&gt;
    
    &lt;?if $(var.Iexist)       ?&gt;&lt;?endif?&gt; <span class="comment">&lt;!-- true --&gt;</span>
    &lt;?if $(var.myValue) = 6  ?&gt;&lt;?endif?&gt; <span class="comment">&lt;!-- false --&gt;</span>
    &lt;?if $(var.myValue)!=3   ?&gt;&lt;?endif?&gt; <span class="comment">&lt;!-- false --&gt;</span>
    &lt;?if not "x"= "y"?&gt;              &lt;?endif?&gt; <span class="comment">&lt;!-- true --&gt;</span>
    &lt;?if $(env.systemdrive)=a?&gt;&lt;?endif?&gt; <span class="comment">&lt;!-- false --&gt;</span>
    &lt;?if 3 &lt; $(var.myValue)?&gt;   &lt;?endif?&gt; <span class="comment">&lt;!-- false --&gt;</span>
    &lt;?if $(var.B) = "good VAR"?&gt; &lt;?endif?&gt; <span class="comment">&lt;!-- false --&gt;</span>
    &lt;?if $(var.A) and not $(env.MyEnvVariable)      ?&gt; &lt;?endif?&gt; <span class="comment">&lt;!-- false --&gt;</span>
    &lt;?if $(var.A) Or ($(var.B) And $(var.myValue) &gt;=3)?&gt;&lt;?endif?&gt; <span class="comment">&lt;!-- true --&gt;</span>
    &lt;?ifdef IExist ?&gt; <span class="comment">&lt;!-- true --&gt;</span>
      &lt;?else?&gt; <span class="comment">&lt;!-- false --&gt;</span>
    &lt;?endif?&gt;
</pre>

## Errors and Warnings

You can use the preprocessor to show meaningful error and warning messages using, &lt;?error error-message ?&gt; and &lt;?warning warning-message?&gt;.&nbsp; When one of these preprocessor instructions is encountered the preprocessor will either display an error and stop the compile or display a warning and continue.

An example:

    <?ifndef RequiredVariable ?>
        <?error RequiredVariable must be defined ?>
    <?endif?>

## Iteration Statements
There is a single iteration statement, &lt;?foreach variable-name in semi-colon-delimited-list ?&gt; &lt;?endforeach?&gt;.&nbsp; When this occurs the preprocessor will

* create a private copy of the variable context
* set the variable in the foreach statement to an iteration on the semicolon delimited list
* generate a fragment with the variable substituted

The effect of this process is that the fragment is used as a template by the preprocessor in order to generate a series of fragments. The variable name in the ?foreach statement can be preceded by &quot;var.&quot;.&nbsp; When a variable is used inside the text of the fragment, it must be preceded by &quot;var.&quot;

An few examples:

    <?foreach LCID in 1033;1041;1055?>
        <Fragment Id='Fragment.$(var.LCID)'>
            <DirectoryRef Id='TARGETDIR'>
                <Component Id='MyComponent.$(var.LCID)' />
            </DirectoryRef>
        </Fragment>
    <?endforeach?>

or

    <?define LcidList=1033;1041;1055?>
    <?foreach LCID in $(var.LcidList)?>
        <Fragment Id='Fragment.$(var.LCID)'>
            <DirectoryRef Id='TARGETDIR'>
                <Component Id='MyComponent.$(var.LCID)' />
            </DirectoryRef>
        </Fragment>
    <?endforeach?>

or

    filename: ExtentOfLocalization.wxi
    <Include>
        <?define LcidList=1033;1041;1055?>
    </Include>

and

    <?include ExtentOfLocalization.wxi ?>
    <?foreach LCID in $(var.LcidList)?>
        <Fragment Id='Fragment.$(var.LCID)'>
            <DirectoryRef Id='TARGETDIR'>
                <Component Id='MyComponent.$(var.LCID)' />
            </DirectoryRef>
        </Fragment>
    <?endforeach?>

An alternative to the foreach process would be to write the template WiX fragment into a separate file and have another process generate the authoring that will be passed to WiX. The greatest merit of this alternative is that it&apos;s easier to debug.

## Escaping

The preprocessor treats the $ character in a special way if it is followed by a $ or (. If you want to use a literal $$, use $$$$ instead. Every two $ characters will be replaced with one. For example, $$$$$ will be replaced with $$$.

##  Functions

The preprocessor supports the following functions:
  
<dl>
  <dt>$(fun.AutoVersion(x.y))</dt>

  <dd>Gets an auto generated version number using the same scheme as .NET AssemblyVersion attribute. The parameters x.y specify the major and minor verion number, the build is set to the number of days since 1/1/2000 and revision to the number of seconds since midnight divided by 2. Both values are calculated using UTC.</dd>
</dl>

## Extensions

WiX has support for preprocessor [extensions](~/wixdev/extensions/extensions.html) via the PreprocessorExtension class. The PreprocessorExtension can provide callbacks with context at foreach initialization, variable evaluation, function definitions, and the last call before invoking the compiler (for full custom preprocessing).
