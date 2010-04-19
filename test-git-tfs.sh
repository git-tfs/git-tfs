#!/bin/sh -x

rm -rf smoke-test || exit
time git tfs clone http://team:8080 $/sandbox smoke-test || exit
cd smoke-test || exit
git tfs fetch || exit
git log --oneline --shortstat | cat
git config -l -f .git/config
echo ok > testfile
git add testfile || exit
git commit -m "Test commit" || exit
git tfs shelve TEST_SHELVESET
git tfs shelve TEST_SHELVESET
git tfs shelve -f TEST_SHELVESET || exit
"/c/Program Files (x86)/Microsoft Visual Studio 9.0/Common7/IDE/TF.exe" shelvesets -format:detailed TEST_SHELVESET
