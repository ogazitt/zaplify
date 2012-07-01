XBUILD=/Developer/usr/bin/xcodebuild
PROJECT_ROOT=SpeechKitLibrary
PROJECT=$(PROJECT_ROOT)/SpeechKitLibrary.xcodeproj
TARGET=SpeechKitLibrary
BTOUCH=/Developer/MonoTouch/usr/bin/btouch
PACKAGES_ROOT=../../../packages/Nuance.SpeechKit.1.0.1/lib/mono

all: SpeechKitLibraryBinder.dll

libSpeechKitLibrary-i386.a:
	$(XBUILD) -project $(PROJECT) -target $(TARGET) -sdk iphonesimulator -configuration Release clean build
	-mv $(PROJECT_ROOT)/build/Release-iphonesimulator/lib$(TARGET).a $@

libSpeechKitLibrary-armv6.a:
	$(XBUILD) -project $(PROJECT) -target $(TARGET) -sdk iphoneos -arch armv6 -configuration Release clean build
	-mv $(PROJECT_ROOT)/build/Release-iphoneos/lib$(TARGET).a $@

libSpeechKitLibrary-armv7.a:
	$(XBUILD) -project $(PROJECT) -target $(TARGET) -sdk iphoneos -arch armv7 -configuration Release clean build
	-mv $(PROJECT_ROOT)/build/Release-iphoneos/lib$(TARGET).a $@

libSpeechKitLibraryUniversal.a: libSpeechKitLibrary-armv7.a libSpeechKitLibrary-armv6.a libSpeechKitLibrary-i386.a
	lipo -create -output $@ $^

SpeechKitLibraryBinder.dll: AssemblyInfo.cs ApiDefinition.cs StructsAndEnums.cs libSpeechKitLibraryUniversal.a
	$(BTOUCH) -unsafe --outdir=tmp -out:$@ ApiDefinition.cs -x=StructsAndEnums.cs -x=AssemblyInfo.cs --link-with=libSpeechKitLibraryUniversal.a,libSpeechKitLibraryUniversal.a
	-mv $@ $(PACKAGES_ROOT)

clean:
	-rm -f *.a *.dll
	-rm -fr bin tmp obj

