export interface DmsAttachmentDto {
  fileId: number;
  fileName: string;
  fileSize: number;
  uploadedBy: string;
  uploadedOn: string | null;
}

export interface DmsAttachmentListResponse {
  purchaseCode: string;
  refType: string;
  refEntryNo: string;
  files: DmsAttachmentDto[];
}
