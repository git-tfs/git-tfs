require 'rubygems'
require 'bundler/setup'
require 'albacore'
require 'rexml/document'
require 'jeweler/version_helper'
require 'git'

task :default => %W(
    build
    runtests
  )

def build task_name, *targets
  build_properties = { :configuration => 'Debug', :platform => 'x86' }
  build_properties.merge!(targets.pop) if Hash === targets.last 
  msbuild task_name do |msb|
    msb.properties build_properties
    msb.targets targets
    msb.solution = 'GitTfs.sln'
  end
end

task :build => 'build:debug'
namespace :build do
  desc "Build in debug mode"
  build :debug, 'Build'
  task :debug

  desc "Build in release mode"
  build :release, 'Build', :configuration => 'Release'
  task :release
end


desc 'Run the tests'
task :runtests do 
  rm 'results.trx' if File.exist? 'results.trx'
  mstest_cmd =%W(mstest
    /testcontainer:GitTfsTest\\bin\\Debug\\GitTfsTest.dll
    /resultsfile:results.trx)
  sh(*mstest_cmd) do |ok, status|
    analyze_tests('results.trx')
  end
end

def analyze_tests(results_file)
  fail_count = 0
  File.open(results_file) do |file|
    xml =REXML::Document.new(file)
    xml.elements.each '//UnitTestResult[@outcome="Failed"]' do |e|
      puts ''
      puts '**********************************'
      puts e.attributes['testName']
      puts e.elements['Output/ErrorInfo/Message'].get_text.value
      puts e.elements['Output/ErrorInfo/StackTrace'].get_text.value
      %W(StdOut StdErr).each do |s|
        show_stream e, s
      end
      puts ''
      fail_count = fail_count.succ
    end
  end
  fail "#{fail_count} tests failed" if fail_count > 0
end

def show_stream(e, s)
  data = e.elements["Output/#{s}"]
  if data
    puts "#{s}:", data.get_text.value
  end
end


desc "Package a release zip"
task :release => ['checks:clean_workdir', 'build:release', 'zip:release', 'tag']

namespace :checks do
  task :clean_workdir do
    unless clean_working_dir?
      system 'git status'
      raise "Unclean working directory! Be sure to commit or .gitignore everything first!"
    end
  end
end

namespace :zip do
  zip :release do |zip|
    zip.directories_to_zip 'GitTfs/bin/Release'
    zip.output_file = "GitTfs-#{version_helper.to_s}.zip"
    zip.output_path = File.expand_path('../pkg', __FILE__)
  end
  task :release => :pkg
end

directory 'pkg'

def version_helper
  @version_helper ||= Jeweler::VersionHelper.new(File.dirname(__FILE__))
end

def repo
  @repo ||= Git.open(File.dirname(__FILE__))
end

def commit opts
  repo.add opts[:files]
  repo.commit opts[:message]
end

def clean_working_dir?
  `git ls-files          --deleted --modified --others --exclude-standard` == ''
end

desc 'Show the current version'
task :version do
  $stdout.puts "Current version: #{version_helper.to_s}"
end

namespace :version do
  task :cs do
    File.open('Version.cs', 'w') do |f|
      f.puts(
        'partial class GitTfsProperties',
        '{',
        "    public const string Version = \"#{version_helper.to_s}\";",
        '}'
      )
    end
    $stdout.puts 'Wrote Version.cs'
    Rake::Task['version:cs'].reenable
  end

  namespace :bump do
    desc "Bump to next major version"
    task :major => :version do
      version_helper.bump_major
      version_helper.write
      $stdout.puts "Updated version to #{version_helper.to_s}"
      Rake::Task['version:cs'].invoke
      commit(:files => ['VERSION', 'Version.cs'], :message => "Bumped to version #{version_helper.to_s}")
    end
    desc "Bump to next minor version"
    task :minor => :version do
      version_helper.bump_minor
      version_helper.write
      $stdout.puts "Updated version to #{version_helper.to_s}"
      Rake::Task['version:cs'].invoke
      commit(:files => ['VERSION', 'Version.cs'], :message => "Bumped to version #{version_helper.to_s}")
    end
    desc "Bump to next patch version"
    task :patch => :version do
      version_helper.bump_patch
      version_helper.write
      $stdout.puts "Updated version to #{version_helper.to_s}"
      Rake::Task['version:cs'].invoke
      commit(:files => ['VERSION', 'Version.cs'], :message => "Bumped to version #{version_helper.to_s}")
    end
  end
end

task :tag do
  release_tag = "v#{version_helper.to_s}"
  tag = repo.tag(release_tag) rescue nil
  unless tag
    $stdout.puts "Tagging #{release_tag}"
    repo.add_tag(release_tag)
  end
end
