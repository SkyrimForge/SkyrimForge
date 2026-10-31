# alandtse's fork, because CharmedBaryon's classifies Skyrim 1.7 as Special Edition and fails the load.
# To move the pin, set REF to the new commit and SHA512 to 0, build, and copy the hash vcpkg reports into SHA512.
vcpkg_from_github(
        OUT_SOURCE_PATH SOURCE_PATH
        REPO alandtse/CommonLibSSE-NG
        # Release v14.0.1.
        REF 723cfda480ae966ee39ab6fb17c4fe7dd0fa4973
        SHA512 8fd9fc5bab3239503ad7280da9dfd32e2ca80b6f75ff765bca04b8f5268734cd91fbbbf6c5f3848fc1d84e7b3529023d46bd713650777f4eb985adb6fcf197a1
        HEAD_REF ng
        PATCHES BGSSaveLoadManager-lastFileName-type.patch TESTriggerEvent-layouts.patch BSPointerHandle-VR-smart-pointer.patch BGSSaveLoadManager-Save-result.patch
                TESObjectCELL-waterObjects-type.patch AE-only-accessor-address.patch Main-runtime-data-2.patch bhkCharacterController-GetRigidBody-type.patch
                BSPortalGraph-portalSharedNode-type.patch hkbCharacterSetup-layout.patch IMenu-VR-runtime-data-offset.patch
                TESBoundObject-virtual-order.patch IDDB-names.patch VariantID-id.patch Menu-base-offsets.patch BGSAcousticSpaceListener-layout.patch
                BSISoundOutputModel-DoGetSpeakerBias-parameters.patch RelocateVirtual-class-return.patch BGSSceneAction-activeFlags.patch
                IDDB-VR-supplement.patch
)

# The entries IDDB-VR-supplement.patch includes, in their own file so an entry is added without regenerating the patch.
file(COPY "${CMAKE_CURRENT_LIST_DIR}/IDDBVRSupplement.inl" DESTINATION "${SOURCE_PATH}/src/REL")

# A submodule of CommonLib, which a source archive omits.
vcpkg_from_github(
        OUT_SOURCE_PATH OPENVR_PATH
        REPO ValveSoftware/openvr
        REF 60eb187801956ad277f1cae6680e3a410ee0873b
        SHA512 bb85b4705e7095ac65df9969112b2df8930cee7917cc5f14231c5a0ffeed7a73ffa60727fd32f8786a403656f95a3ec0f80bf3ceabc5b8ede964aefb920bc718
        HEAD_REF master
)
file(COPY "${OPENVR_PATH}/." DESTINATION "${SOURCE_PATH}/extern/openvr")

# CommonLib fetches its decoder from MinHook through FetchContent, which cannot download under vcpkg.
vcpkg_from_github(
        OUT_SOURCE_PATH HDE64_PATH
        REPO TsudaKageyu/minhook
        REF v1.3.4
        SHA512 8a33233598b56ad9da44d22d470c2432f68364dac31bc719fcd6b085e681fa10ddd41865fbde056ee7f4e7a075cc135344b6bf444eadbd7e7314ee1bedfd89b5
        HEAD_REF master
)

vcpkg_cmake_configure(
        SOURCE_PATH "${SOURCE_PATH}"
        OPTIONS
                -DBUILD_TESTS=off
                -DSKSE_SUPPORT_XBYAK=on
                "-DFETCHCONTENT_SOURCE_DIR_HDE64=${HDE64_PATH}"
)

vcpkg_cmake_install()
vcpkg_cmake_config_fixup(PACKAGE_NAME CommonLibSSE CONFIG_PATH lib/cmake)
vcpkg_copy_pdbs()

file(GLOB CMAKE_CONFIGS "${CURRENT_PACKAGES_DIR}/share/CommonLibSSE/CommonLibSSE/*.cmake")
file(INSTALL ${CMAKE_CONFIGS} DESTINATION "${CURRENT_PACKAGES_DIR}/share/CommonLibSSE")
file(INSTALL "${SOURCE_PATH}/cmake/CommonLibSSE.cmake" DESTINATION "${CURRENT_PACKAGES_DIR}/share/CommonLibSSE")

# RE/B/BSVRInterface.h includes openvr.h, and the binding generator parses every installed header.
file(INSTALL "${OPENVR_PATH}/headers/" DESTINATION "${CURRENT_PACKAGES_DIR}/include")

# The binding generator reads each function's RELOCATION_ID from these sources.
file(INSTALL "${SOURCE_PATH}/src/RE/" DESTINATION "${CURRENT_PACKAGES_DIR}/share/${PORT}/src/RE" FILES_MATCHING PATTERN "*.cpp")

file(REMOVE_RECURSE "${CURRENT_PACKAGES_DIR}/debug/include")
file(REMOVE_RECURSE "${CURRENT_PACKAGES_DIR}/share/CommonLibSSE/CommonLibSSE")

vcpkg_install_copyright(FILE_LIST "${SOURCE_PATH}/COPYING.txt" "${SOURCE_PATH}/EXCEPTIONS.md")
