# ??? ���� ���̵� - �����ͺ��̽� ����

## ?? �߿�: ���� �����ͺ��̽� ������ ���� GitHub�� ���ε����� ������!

### ȯ�溯�� ���� ���

#### 1. Windows ȯ�溯�� ����
```cmd
# �ý��� ȯ�溯�� ���� (������ ���� �ʿ�)
setx DB_HOST "��������IP" /M
setx DB_PORT "3306" /M  
setx DB_NAME "masinsave" /M
setx DB_USER "����ڸ�" /M
setx DB_PASSWORD "��й�ȣ" /M
setx DB_TIMEOUT "30" /M
setx DB_USE_SSL "false" /M
```

#### 2. ���� �� ���� ���� ����
���� ���� �ÿ��� `appsettings.json` ������ ������ �����Ͽ� ����ϼ���:

```json
{
  "DatabaseSettings": {
    "Host": "��������IP",
    "Port": 3306,
    "Database": "masinsave", 
    "UserId": "��������ڸ�",
    "Password": "������й�ȣ",
    "ConnectionTimeout": 30,
    "UseSSL": false
  }
}
```

#### 3. Git���� ������ ���ϵ�
���� ���ϵ��� �ݵ�� `.gitignore`�� ���ԵǾ�� �մϴ�:
- `appsettings.json`
- `config.json`
- `database.config`
- `Models/AppSettings.cs` (���� ������ �ϵ��ڵ��� ���)

### ������ ���̵�

#### ���� ���� ȯ��
- �⺻���� `localhost`�� �����Ǿ� �ֽ��ϴ�
- ���� MySQL ������ ����Ͽ� �����ϼ���

#### ���δ��� ȯ��  
- ȯ�溯���� ���� ������ ���� ���� ���� ������ �����ϼ���
- ���� �ϵ��ڵ����� ������!

### ���� üũ����Ʈ
- [ ] ���� ���� IP �ϵ��ڵ� ����
- [ ] ���� ��й�ȣ �ϵ��ڵ� ����  
- [ ] .gitignore�� ���� ���ϵ� �߰�
- [ ] ȯ�溯�� ���� �Ϸ�
- [ ] ���� �� ���� ���� �Ϸ�