require 'albacore'
require 'rexml/document'

module Rake
  class MSBuildTask
    def define
      task name do
        # This is in latest of albacore, so it can eventually be removed from here.
        fail unless @msbuild.build
      end
    end
  end
end

task :default => %W(
    build
    runtests
  )

def build task_name, *targets
  build_properties = { :configuration => 'Debug', :platform => 'x86' }
  build_properties.merge!(targets.pop) if Hash === targets.last 
  Rake::MSBuildTask.new(task_name) do |msb|
    msb.properties build_properties
    msb.targets targets
    msb.solution = 'GitTfs.sln'
  end
end

desc 'Build the entire solution'
build :build, 'Build'

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
