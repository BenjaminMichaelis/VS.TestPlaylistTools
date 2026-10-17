#!/bin/sh
set -eu
work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT
export NUGET_PACKAGES="$work/cache"
unset DOTNET_RUNTIME_ID DOTNET_ROLL_FORWARD DOTNET_ROLL_FORWARD_TO_PRERELEASE
cat > "$work/nuget.config" <<'EOF'
<configuration><packageSources><clear/><add key="artifacts" value="/src/packed"/></packageSources></configuration>
EOF
dotnet tool install trx-to-vsplaylist --version "$PACKAGE_VERSION" --tool-path "$work/tools" --configfile "$work/nuget.config"
test -d "$work/tools/.store/trx-to-vsplaylist/$PACKAGE_VERSION/trx-to-vsplaylist.$TARGET_RID/$PACKAGE_VERSION"
tool="$work/tools/trx-to-vsplaylist"
"$tool" --version | grep -F "$PACKAGE_VERSION"
"$tool" --help > "$work/help"
grep -q convert "$work/help"
grep -q merge "$work/help"
cat > "$work/first.trx" <<'EOF'
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <TestDefinitions>
    <UnitTest name="One" id="one"><TestMethod className="Ns.Tests" name="One" /></UnitTest>
    <UnitTest name="Two" id="two"><TestMethod className="Ns.Tests" name="Two" /></UnitTest>
  </TestDefinitions>
  <Results>
    <UnitTestResult testId="one" testName="One" outcome="Passed" />
    <UnitTestResult testId="two" testName="Two" outcome="Failed" />
  </Results>
</TestRun>
EOF
cp "$work/first.trx" "$work/second.trx"
assert_tests() {
    file=$1
    expected=$2
    count=$(grep -o '<Add ' "$file" | wc -l)
    test "$count" -eq "$expected"
}
"$tool" convert "$work/first.trx" --output "$work/all.playlist"
assert_tests "$work/all.playlist" 2
grep -q 'Test="Ns.Tests.One"' "$work/all.playlist"
grep -q 'Test="Ns.Tests.Two"' "$work/all.playlist"
"$tool" convert "$work/first.trx" --outcome Failed --output "$work/failed.playlist"
assert_tests "$work/failed.playlist" 1
grep -q 'Test="Ns.Tests.Two"' "$work/failed.playlist"
"$tool" convert "$work/first.trx" "$work/second.trx" --output "$work/merged.playlist"
assert_tests "$work/merged.playlist" 2
"$tool" convert "$work/first.trx" "$work/second.trx" --separate --output "$work/separate"
assert_tests "$work/separate/first.playlist" 2
assert_tests "$work/separate/second.playlist" 2
"$tool" convert "$work/first.trx" --outcome NotExecuted --skip-empty --output "$work/empty.playlist"
test ! -e "$work/empty.playlist"
"$tool" merge "$work/separate/*.playlist" --output "$work/merged.playlist"
assert_tests "$work/merged.playlist" 2
expect_failure() {
    if "$tool" "$@"; then
        echo "Expected failure: $*" >&2
        exit 1
    fi
}
expect_failure convert "$work/missing.trx"
expect_failure convert "$work/first.trx" --outcome InvalidOutcome
expect_failure --not-an-option
# TrxLib currently treats malformed input as an empty test run.
printf '<TestRun><invalid' > "$work/bad.trx"
"$tool" convert "$work/bad.trx" --output "$work/empty.playlist"
assert_tests "$work/empty.playlist" 0
echo "Alpine installed-package scenarios passed for $TARGET_RID"
