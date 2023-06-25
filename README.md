# ShellProtect

## 한국어

### **VRChat에서 사용 가능한 쉐이더를 이용한 텍스쳐 암호화**

텍스쳐를 암호화 시킨 후, 쉐이더를 이용하여 텍스쳐를 복호화합니다.

아바타 복사는 막지 못하지만, 리핑을 통해 아바타의 텍스쳐를 뜯어가서 수정하는 것을 막을 수 있습니다.

복사를 막고 싶다면 비밀번호 기능과 함께 쓰십시오.

### 세부 원리
변형된 XTEA 알고리즘을 사용하여 텍스쳐를 암호화합니다.

텍스쳐 자체를 암호화 한 후 VRChat 서버에 업로드 됩니다. 이 텍스쳐는 게임에서 쉐이더를 통해 복호화 시킵니다.

### 지원 쉐이더
- Poiyomi 8.2
- 포이요미 다른 버전 지원 예정

## English

### **Texture encryption using shaders available in VRChat**.

After encrypting the texture, the shader is used to decrypt the texture.

This does not prevent copying of the avatar, but it does prevent ripping and modifying the avatar's texture.

If you want to prevent copying, use it in conjunction with the password feature.

### How it works
Encrypt the texture using a modified XTEA algorithm.

After encrypting the texture itself, it is uploaded to the VRChat server. The texture is then decrypted in the game via shaders.

### Supported shaders
- Poiyomi 8.2
- Support for other Poiyomi versions coming soon